using System;
using System.IO;
using System.Linq.Expressions;
using System.Reflection;

namespace Shaolinq.Parser
{
	public class ComputedExpressionParser<OBJECT_TYPE, PROPERTY_TYPE>
	{
		private ComputedExpressionToken token;
		private readonly PropertyInfo propertyInfo;
		private readonly ParameterExpression targetObject;
		private readonly ComputedExpressionTokenizer tokenizer;
		
		public ComputedExpressionParser(TextReader reader, PropertyInfo propertyInfo)
		{
			this.propertyInfo = propertyInfo;
			this.tokenizer = new ComputedExpressionTokenizer(reader);
			this.targetObject = Expression.Parameter(typeof(OBJECT_TYPE), "object");
		}

		public void Consume()
		{
			tokenizer.ReadNextToken();
			token = tokenizer.CurrentToken;
		}

		public virtual Expression<Func<OBJECT_TYPE, PROPERTY_TYPE>> Parse()
		{
			this.Consume();

			var body = this.ParseExpression();

			var retval = Expression.Lambda<Func<OBJECT_TYPE, PROPERTY_TYPE>>(body, targetObject);

			return retval;
		}

		protected virtual Expression ParseExpression()
		{
			return this.ParseComparison();
		}

		protected Expression ParseUnary()
		{
			if (token == ComputedExpressionToken.Subtract)
			{
				this.Consume();

				return Expression.Multiply(Expression.Constant(-1), ParseOperand());
			}

			return this.ParseOperand();
		}

		protected Expression ParseComparison()
		{
			var leftOperand = ParseAddOrSubtract();
			var retval = leftOperand;

			while (token >= ComputedExpressionToken.CompareStart && token <= ComputedExpressionToken.CompareEnd)
			{
				var operationToken = token;

				Consume();

				var rightOperand = ParseAddOrSubtract();

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

				Consume();
			}

			return retval;
		}

		protected Expression ParseAddOrSubtract()
		{
			var leftOperand = ParseMultiplyOrDivide();
			var retval = leftOperand;

			while (token == ComputedExpressionToken.Add || token == ComputedExpressionToken.Subtract)
			{
				var operationToken = token;

				Consume();

				var rightOperand = ParseMultiplyOrDivide();

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
			var leftOperand = ParseUnary();
			var retval = leftOperand;

			while (token == ComputedExpressionToken.Multiply || token == ComputedExpressionToken.Divide)
			{
				var operationToken = token;

				Consume();

				var rightOperand = ParseUnary();

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

		protected Expression ParseOperand()
		{
			if (this.token == ComputedExpressionToken.LeftParen)
			{
				this.Consume();

				var retval = this.ParseExpression();

				this.Consume();

				this.Expect(ComputedExpressionToken.RightParen);

				return retval;
			}

			Expression current = targetObject;


			if (this.token == ComputedExpressionToken.Identifier)
			{
				while (true)
				{
					var identifier = this.tokenizer.CurrentIdentifier;

					this.Consume();

					if (this.token == ComputedExpressionToken.LeftParen)
					{
						throw new NotSupportedException();
					}
					else
					{
						current = Expression.PropertyOrField(current, identifier);
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
