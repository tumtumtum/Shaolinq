using System;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using System.Reflection;

namespace Shaolinq.Persistence.Computed
{
	public class ComputedExpressionParser
	{
		private ComputedExpressionToken token;
		private readonly PropertyInfo propertyInfo;
		private readonly ParameterExpression targetObject;
		private readonly ComputedExpressionTokenizer tokenizer;

		public ComputedExpressionParser(TextReader reader, PropertyInfo propertyInfo)
		{
			this.propertyInfo = propertyInfo;
			this.tokenizer = new ComputedExpressionTokenizer(reader);
			this.targetObject = Expression.Parameter(propertyInfo.DeclaringType, "object");
		}

		public void Consume()
		{
			this.tokenizer.ReadNextToken();
			this.token = this.tokenizer.CurrentToken;
		}

		public static LambdaExpression Parse(string expressionText, PropertyInfo propertyInfo)
		{
			return Parse(new StringReader(expressionText), propertyInfo);
		}

		public static LambdaExpression Parse(TextReader reader, PropertyInfo propertyInfo)
		{
			return new ComputedExpressionParser(reader, propertyInfo).Parse();
		}

		public virtual LambdaExpression Parse()
		{
			this.Consume();

			var body = this.ParseExpression();

			if (body.Type != this.propertyInfo.PropertyType)
			{
				body = Expression.Convert(body, this.propertyInfo.PropertyType);
			}

			var retval = Expression.Lambda(body, this.targetObject);

			return retval;
		}

		protected virtual Expression ParseExpression()
		{
			return this.ParseComparison();
		}

		protected Expression ParseUnary()
		{
			if (this.token == ComputedExpressionToken.Subtract)
			{
				this.Consume();

				return Expression.Multiply(Expression.Constant(-1), this.ParseOperand());
			}

			return this.ParseOperand();
		}

		protected Expression ParseComparison()
		{
			var leftOperand = this.ParseNullCoalescing();
			var retval = leftOperand;

			while (this.token >= ComputedExpressionToken.CompareStart && this.token <= ComputedExpressionToken.CompareEnd)
			{
				var operationToken = this.token;

				this.Consume();

				var rightOperand = this.ParseNullCoalescing();

				switch (operationToken)
				{
				case ComputedExpressionToken.Equals:
					retval = Expression.Equal(leftOperand, rightOperand);
					break;
				case ComputedExpressionToken.NotEquals:
					retval = Expression.NotEqual(leftOperand, rightOperand);
					break;
				case ComputedExpressionToken.GreaterThan:
					retval = Expression.GreaterThan(leftOperand, rightOperand);
					break;
				case ComputedExpressionToken.GreaterThanOrEqual:
					retval = Expression.GreaterThanOrEqual(leftOperand, rightOperand);
					break;
				case ComputedExpressionToken.LessThan:
					retval = Expression.LessThan(leftOperand, rightOperand);
					break;
				case ComputedExpressionToken.LessThanOrEqual:
					retval = Expression.LessThanOrEqual(leftOperand, rightOperand);
					break;
				}

				this.Consume();
			}

			return retval;
		}



		protected Expression ParseNullCoalescing()
		{
			var leftOperand = this.ParseAddOrSubtract();
			var retval = leftOperand;

			if (this.token == ComputedExpressionToken.DoubleQuestionMark)
			{
				this.Consume();

				var rightOperand = this.ParseAddOrSubtract();

				return Expression.Coalesce(leftOperand, rightOperand);
			}

			return retval;
		}

		protected Expression ParseAddOrSubtract()
		{
			var leftOperand = this.ParseMultiplyOrDivide();
			var retval = leftOperand;

			while (this.token == ComputedExpressionToken.Add || this.token == ComputedExpressionToken.Subtract)
			{
				var operationToken = this.token;

				this.Consume();

				var rightOperand = this.ParseMultiplyOrDivide();

				if (operationToken == ComputedExpressionToken.Add)
				{
					retval = Expression.Add(leftOperand, rightOperand);
				}
				else
				{
					retval = Expression.Subtract(leftOperand, rightOperand);
				}

				this.Consume();
			}

			return retval;
		}

		protected Expression ParseMultiplyOrDivide()
		{
			var leftOperand = this.ParseUnary();
			var retval = leftOperand;

			while (this.token == ComputedExpressionToken.Multiply || this.token == ComputedExpressionToken.Divide)
			{
				var operationToken = this.token;

				this.Consume();

				var rightOperand = this.ParseUnary();

				if (operationToken == ComputedExpressionToken.Multiply)
				{
					retval = Expression.Multiply(leftOperand, rightOperand);
				}
				else
				{
					retval = Expression.Divide(leftOperand, rightOperand);
				}
			}

			return retval;
		}

		protected void Expect(ComputedExpressionToken tokenValue)
		{
			if (this.token != tokenValue)
			{
				throw new InvalidOperationException("Expected token: " + tokenValue);
			}
		}

		protected Expression ParseMethodCall(Expression target, string methodName)
		{
			this.Consume();
			var arguments = new List<Expression>();

			while (true)
			{
				if (this.token == ComputedExpressionToken.RightParen)
				{
					break;
				}

				var argument = this.ParseExpression();

				arguments.Add(argument);

				if (this.token == ComputedExpressionToken.Comma)
				{
					this.Consume();

					continue;
				}
				else if (this.token != ComputedExpressionToken.RightParen)
				{
					throw new InvalidOperationException("Expected ')'");
				}

				break;
			}

			return Expression.Call(target, methodName, null, arguments.ToArray());
		}

		protected Expression ParseOperand()
		{
			if (this.token == ComputedExpressionToken.LeftParen)
			{
				this.Consume();

				var retval = this.ParseExpression();

				this.Expect(ComputedExpressionToken.RightParen);

				return retval;
			}

			Expression current = this.targetObject;

			if (this.token == ComputedExpressionToken.Identifier)
			{
				while (true)
				{
					var identifier = this.tokenizer.CurrentIdentifier;

					this.Consume();

					if (this.token == ComputedExpressionToken.LeftParen)
					{
						current = this.ParseMethodCall(current, identifier);
					}
					else
					{
						if (identifier == "value" && current.Type.GetField("value") == null)
						{
							current = Expression.Property(this.targetObject, this.propertyInfo);
						}
						else
						{
							current = Expression.PropertyOrField(current, identifier);
						}
					}

					if (this.token == ComputedExpressionToken.Period)
					{
						this.Consume();

						if (this.token != ComputedExpressionToken.Identifier)
						{
							throw new InvalidOperationException();
						}

						continue;
					}

					return current;
				}
			}


			if (this.token == ComputedExpressionToken.IntegerLiteral)
			{
				this.Consume();

				return Expression.Constant(this.tokenizer.CurrentInteger);
			}
			else if (this.token == ComputedExpressionToken.StringLiteral)
			{
				this.Consume();

				return Expression.Constant(this.tokenizer.CurrentString);
			}
			
			throw new InvalidOperationException();
		}
	}
}
