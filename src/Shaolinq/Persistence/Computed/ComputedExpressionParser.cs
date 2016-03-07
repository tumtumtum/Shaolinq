// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Platform;
using Shaolinq.Persistence.Linq;

namespace Shaolinq.Persistence.Computed
{
	public class ComputedExpressionParser
	{
		private ComputedExpressionToken token;
		private readonly PropertyInfo propertyInfo;
		private readonly List<Assembly> referencedAssemblies;
		private readonly ParameterExpression targetObject;
		private readonly ComputedExpressionTokenizer tokenizer;
		
		public ComputedExpressionParser(TextReader reader, PropertyInfo propertyInfo, Type[] referencedTypes = null)
		{
			if (propertyInfo.DeclaringType == null)
			{
				throw new ArgumentException(nameof(propertyInfo));
			}

			this.propertyInfo = propertyInfo;
			this.referencedAssemblies = new HashSet<Assembly>(referencedTypes?.Select(c => c.Assembly) ?? new Assembly[0]).ToList();
			this.tokenizer = new ComputedExpressionTokenizer(reader);
			this.targetObject = Expression.Parameter(propertyInfo.DeclaringType, "object");
		}

		public static LambdaExpression Parse(TextReader reader, PropertyInfo propertyInfo, Type[] referencedTypes) => new ComputedExpressionParser(reader, propertyInfo, referencedTypes).Parse();
		public static LambdaExpression Parse(string expressionText, PropertyInfo propertyInfo, Type[] referencedTypes) => Parse(new StringReader(expressionText), propertyInfo, referencedTypes);

		public void Consume()
		{
			this.tokenizer.ReadNextToken();
			this.token = this.tokenizer.CurrentToken;
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
			return this.ParseAssignment();
		}

		protected Expression ParseAssignment()
		{
			var leftOperand = this.ParseNullCoalescing();
			var retval = leftOperand;

			while (this.token == ComputedExpressionToken.Assign)
			{
				this.Consume();

				var rightOperand = this.ParseNullCoalescing();

				if (rightOperand.Type != retval.Type)
				{
					rightOperand = Expression.Convert(rightOperand, retval.Type);
				}

				retval = Expression.Assign(retval, rightOperand);
			}

			return retval;
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

		protected void NormalizeOperands(ref Expression left, ref Expression right)
		{
			left = left.UnwrapNullable();
			right = right.UnwrapNullable();

			if (left.Type == typeof(byte) &&
				(right.Type == typeof(long) || right.Type == typeof(double) || right.Type == typeof(decimal)))
			{
				left = Expression.Convert(left, right.Type);
			}
			else if (left.Type == typeof(char) && 
				(right.Type == typeof(long) || right.Type == typeof(double) || right.Type == typeof(decimal)))
			{
				left = Expression.Convert(left, right.Type);
			}
			else if (left.Type == typeof(short) &&
				(right.Type == typeof(long) || right.Type == typeof(double) || right.Type == typeof(decimal)))
			{
				left = Expression.Convert(left, right.Type);
			}
			else if (left.Type == typeof(int) &&
				(right.Type == typeof(long) || right.Type == typeof(double) || right.Type == typeof(decimal)))
			{
				left = Expression.Convert(left, right.Type);
			}
			else if (left.Type == typeof(long) &&
				(right.Type == typeof(long) || right.Type == typeof(double) || right.Type == typeof(decimal)))
			{
				left = Expression.Convert(left, right.Type);
			}
			else if (right.Type == typeof(byte) &&
				(left.Type == typeof(long) || left.Type == typeof(double) || left.Type == typeof(decimal)))
			{
				right = Expression.Convert(right, left.Type);
			}
			else if (right.Type == typeof(char) &&
				(left.Type == typeof(long) || left.Type == typeof(double) || left.Type == typeof(decimal)))
			{
				right = Expression.Convert(right, left.Type);
			}
			else if (right.Type == typeof(short) &&
				(left.Type == typeof(long) || left.Type == typeof(double) || left.Type == typeof(decimal)))
			{
				right = Expression.Convert(right, left.Type);
			}
			else if (right.Type == typeof(int) &&
				(left.Type == typeof(long) || left.Type == typeof(double) || left.Type == typeof(decimal)))
			{
				right = Expression.Convert(right, left.Type);
			}
			else if (right.Type == typeof(long) &&
				(left.Type == typeof(long) || left.Type == typeof(double) || left.Type == typeof(decimal)))
			{
				right = Expression.Convert(right, left.Type);
			}
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

				NormalizeOperands(ref leftOperand, ref rightOperand);

				if (operationToken == ComputedExpressionToken.Add)
				{
					retval = Expression.Add(leftOperand, rightOperand);
				}
				else if (operationToken == ComputedExpressionToken.Subtract)
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

				NormalizeOperands(ref leftOperand, ref rightOperand);

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

			this.Consume();

			if (target.NodeType == ExpressionType.Constant && target.Type == typeof(TypeHolder))
			{
				var type = ((target as ConstantExpression).Value as TypeHolder).Type;

				var method = FindMatchingMethod(type, methodName, arguments.Select(c => c.Type).ToArray(), true);

				if (method == null)
				{
					throw new InvalidOperationException($"Unable to find static method named '{methodName}' in type {type}");
				}

				return Expression.Call(method, arguments.ToArray());
			}
			else
			{
				var method = FindMatchingMethod(target.Type, methodName, arguments.Select(c => c.Type).ToArray(), false);

				if (method == null)
				{
					throw new InvalidOperationException($"Unable to find instance method named '{methodName}' in type {target.Type.FullName}");
				}

				return Expression.Call(target, method, arguments.ToArray());
			}
		}
		
		private MethodInfo FindMatchingMethod(Type type, string methodName, Type[] argumentTypes, bool isStatic)
		{
			var bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy | (isStatic ? BindingFlags.Static : BindingFlags.Instance);

			var match = type.GetMethod(methodName, bindingFlags, null, argumentTypes, null);

			if (match != null)
			{
				return match;
			}

			var methods = type
				.GetMethods(bindingFlags)
				.Where(c => c.Name == methodName)
				.Where(c => c.IsGenericMethod)
				.Where(c => c.GetParameters().Length == argumentTypes.Length);

			foreach (var method in methods)
			{
				var parameters = method.GetParameters();
				var genericArgs = method.GetGenericArguments();
				var newArgs = (Type[])argumentTypes.Clone();
				var realisedGenericArgs = new List<Type>();

				foreach (var genericArg in genericArgs)
				{
					var index = Array.FindIndex(parameters, c => c.ParameterType == genericArg);

					if (index >= 0 && parameters[index].ParameterType.GetGenericParameterConstraints().All(c => c.IsAssignableFrom(argumentTypes[index])))
					{
						realisedGenericArgs.Add(newArgs[index]);
						newArgs[index] = parameters[index].ParameterType;
					}
				}

				match = type.GetMethod(methodName, bindingFlags, null, newArgs, null);

				if (match != null)
				{
					match = match.MakeGenericMethod(realisedGenericArgs.ToArray());

					return match;
				}
			}

			return null;
		}

		private bool TryGetType(string name, out Type value)
		{
			var type = Type.GetType(name);

			if (type != null)
			{
				value = type;

				return true;
			}

			foreach (var assembly in this.referencedAssemblies)
			{
				if ((type = assembly.GetType(name)) != null)
				{
					value = type;

					return true;
				}
			}

			value = null;

			return false;
		}

		private class TypeHolder
		{
			public Type Type { get; set; }

			public TypeHolder(Type type)
			{
				this.Type = type;
			}
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

			var identifierStack = new Stack<string>();
			Expression current = null;

			if (this.token == ComputedExpressionToken.Keyword && this.tokenizer.CurrentKeyword == ComputedExpressionKeyword.This)
			{
				current = this.targetObject;
				this.Consume();
			}
			
			if (this.token == ComputedExpressionToken.Identifier)
			{
				while (true)
				{
					var identifier = this.tokenizer.CurrentIdentifier;

					this.Consume();

					if (this.token == ComputedExpressionToken.LeftParen)
					{
						current = current ?? this.targetObject;

						current = this.ParseMethodCall(current, identifier);
					}
					else
					{
						var bindingFlags = BindingFlags.Public;
						var currentWasNull = current == null;

						current = current ?? this.targetObject;
						
						if (current.NodeType == ExpressionType.Constant && current.Type == typeof(TypeHolder))
						{
							bindingFlags |= BindingFlags.Static;
						}
						else
						{
							bindingFlags |= BindingFlags.Instance;
						}

						MemberInfo member;

						if (identifier == "value" && current == this.targetObject)
						{
							current = Expression.Property(current, this.propertyInfo);
							identifierStack.Clear();
							identifierStack.Push(identifier);
						}
						else if ((member = current.Type.GetProperty(identifier, bindingFlags)) != null
								 || (member = current.Type.GetField(identifier, bindingFlags)) != null)
						{
							var expression = (bindingFlags & BindingFlags.Static) != 0 ? null : current;

							current = Expression.MakeMemberAccess(expression, member);

							identifierStack.Clear();
							identifierStack.Push(identifier);
						}
						else
						{
							Type type;
							var fullIdentifierName = string.Join(".", identifierStack.Reverse());

							identifierStack.Push(identifier);

							if (current.NodeType == ExpressionType.Constant && ((current as ConstantExpression).Value as TypeHolder)?.Type.FullName == fullIdentifierName)
							{
								fullIdentifierName += (fullIdentifierName == "" ? "" : "+") + identifierStack.First();
							}
							else
							{
								fullIdentifierName += (fullIdentifierName == "" ? "" : ".") + identifierStack.First();
							}

							if (TryGetType(fullIdentifierName, out type))
							{
								current = Expression.Constant(new TypeHolder(type));
							}
							else
							{
								if (currentWasNull)
								{
									current = Expression.Constant(new TypeHolder(targetObject.Type));
								}
							}
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
			else if (current != null)
			{
				return current;
			}
			
			throw new InvalidOperationException();
		}
	}
}
