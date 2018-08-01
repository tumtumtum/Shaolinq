// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Platform;

namespace Shaolinq.Persistence.Computed
{
	public class ComputedExpressionParser
	{
		private ComputedExpressionToken token;
		private readonly PropertyInfo propertyInfo;
		private readonly Type coersionType;
		private readonly List<Assembly> referencedAssemblies;
		private readonly ParameterExpression targetObject;
		private readonly ComputedExpressionTokenizer tokenizer;
		private readonly Dictionary<string, Type> referencedTypesByName;

		public ComputedExpressionParser(TextReader reader, PropertyInfo propertyInfo, ParameterExpression target, Type[] referencedTypes = null, Type coersionType = null)
		{
			if (propertyInfo.DeclaringType == null)
			{
				throw new ArgumentException(nameof(propertyInfo));
			}

			this.propertyInfo = propertyInfo;
			this.coersionType = coersionType;
			this.referencedTypesByName = (referencedTypes ?? new Type[0]).ToDictionary(c => c.Name, c => c);
			this.referencedAssemblies = new HashSet<Assembly>(referencedTypes?.Select(c => c.Assembly) ?? new Assembly[0]).ToList();
			this.tokenizer = new ComputedExpressionTokenizer(reader);
			this.targetObject = target;
		}

		public static LambdaExpression Parse(TextReader reader, PropertyInfo propertyInfo, ParameterExpression target, Type[] referencedTypes, Type coersionType)
			=> new ComputedExpressionParser(reader, propertyInfo, target, referencedTypes, coersionType).Parse();

		public static LambdaExpression Parse(string expressionText, PropertyInfo propertyInfo, ParameterExpression target, Type[] referencedTypes, Type coersionType)
			=> new ComputedExpressionParser(new StringReader(expressionText), propertyInfo, target, referencedTypes, coersionType).Parse();

		public static LambdaExpression Parse(TextReader reader, PropertyInfo propertyInfo, Type[] referencedTypes, Type coersionType)
			=> Parse(reader, propertyInfo, Expression.Parameter(propertyInfo.DeclaringType, "targetObject"), referencedTypes, coersionType);

		public static LambdaExpression Parse(string expressionText, PropertyInfo propertyInfo, Type[] referencedTypes, Type coersionType)
			=> Parse(new StringReader(expressionText), propertyInfo, referencedTypes, coersionType);

		public void Consume()
		{
			this.tokenizer.ReadNextToken();
			this.token = this.tokenizer.CurrentToken;
		}

		protected virtual LambdaExpression Parse()
		{
			Consume();

			var body = ParseExpression();

			if (body == null)
			{
				return null;
			}

			if (this.coersionType != null && body.Type != this.coersionType)
			{
				body = Expression.Convert(body, this.coersionType);
			}

			var retval = Expression.Lambda(body, this.targetObject);

			return retval;
		}

		protected virtual Expression ParseExpression()
		{
			return ParseAssignment();
		}

		protected Expression ParseAssignment()
		{
			var leftOperand = ParseAndOr();
			var retval = leftOperand;

			while (this.token == ComputedExpressionToken.Assign)
			{
				Consume();

				var rightOperand = ParseAndOr();

				if (rightOperand.Type != retval.Type)
				{
					rightOperand = Expression.Convert(rightOperand, retval.Type);
				}

				retval = Expression.Assign(retval, rightOperand);
			}

			return retval;
		}

		protected Expression ParseAndOr()
		{
			var leftOperand = ParseNullCoalescing();
			var retval = leftOperand;

			while (this.token == ComputedExpressionToken.LogicalAnd || this.token == ComputedExpressionToken.LogicalOr)
			{
				var operationToken = this.token;

				Consume();

				var rightOperand = ParseNullCoalescing();

				NormalizeOperandsForArithmatic(ref leftOperand, ref rightOperand);

				if (operationToken == ComputedExpressionToken.LogicalAnd)
				{
					retval = Expression.And(leftOperand, rightOperand);
				}
				else if (operationToken == ComputedExpressionToken.LogicalOr)
				{
					retval = Expression.Or(leftOperand, rightOperand);
				}
			}

			return retval;
		}

		protected Expression ParseUnary()
		{
			if (this.token == ComputedExpressionToken.Subtract)
			{
				Consume();

				return Expression.Multiply(Expression.Constant(-1), ParseOperand());
			}

			return ParseOperand();
		}

		protected Expression ParseComparison()
		{
			var leftOperand = ParseAddOrSubtract();
			var retval = leftOperand;

			while (this.token >= ComputedExpressionToken.CompareStart && this.token <= ComputedExpressionToken.CompareEnd)
			{
				var operationToken = this.token;

				Consume();

				var rightOperand = ParseAddOrSubtract();

				NormalizeOperandsForArithmatic(ref leftOperand, ref rightOperand);

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
			}

			return retval;
		}

		protected Expression ParseNullCoalescing()
		{
			var leftOperand = ParseComparison();
			var retval = leftOperand;

			if (this.token == ComputedExpressionToken.DoubleQuestionMark)
			{
				Consume();

				var rightOperand = ParseComparison();

				return Expression.Coalesce(leftOperand, rightOperand);
			}

			return retval;
		}

		protected void NormalizeOperandsForArithmatic(ref Expression left, ref Expression right)
		{
			var leftUnwrappedType = left.Type.GetUnwrappedNullableType();
			var rightUnwrappedType = right.Type.GetUnwrappedNullableType();

			if (leftUnwrappedType == rightUnwrappedType && (left.Type != right.Type) && (left.Type.IsNullableType() || right.Type.IsNullableType()))
			{
				if (right.Type.IsNullableType())
				{
					left = Expression.Convert(left, right.Type);
				}
				else
				{
					right = Expression.Convert(right, left.Type);
				}
			}

			if (leftUnwrappedType == typeof(byte) &&
				(rightUnwrappedType == typeof(short) || rightUnwrappedType == typeof(int) || rightUnwrappedType == typeof(long) || rightUnwrappedType == typeof(double) || rightUnwrappedType == typeof(decimal)))
			{
				left = Expression.Convert(left, right.Type);
			}
			else if (leftUnwrappedType == typeof(char) && 
				(rightUnwrappedType == typeof(short) || rightUnwrappedType == typeof(int) || rightUnwrappedType == typeof(long) || rightUnwrappedType == typeof(double) || rightUnwrappedType == typeof(decimal)))
			{
				left = Expression.Convert(left, right.Type);
			}
			else if (leftUnwrappedType == typeof(short) &&
				(rightUnwrappedType == typeof(long) || rightUnwrappedType == typeof(double) || rightUnwrappedType == typeof(decimal)))
			{
				left = Expression.Convert(left, right.Type);
			}
			else if (leftUnwrappedType == typeof(int) &&
				(rightUnwrappedType == typeof(long) || rightUnwrappedType == typeof(double) || rightUnwrappedType == typeof(decimal)))
			{
				left = Expression.Convert(left, right.Type);
			}
			else if (leftUnwrappedType == typeof(long) &&
				(rightUnwrappedType == typeof(double) || rightUnwrappedType == typeof(decimal)))
			{
				left = Expression.Convert(left, right.Type);
			}
			else if (rightUnwrappedType == typeof(byte) &&
				(leftUnwrappedType == typeof(short) || leftUnwrappedType == typeof(int) || leftUnwrappedType == typeof(long) || leftUnwrappedType == typeof(double) || leftUnwrappedType == typeof(decimal)))
			{
				right = Expression.Convert(right, left.Type);
			}
			else if (rightUnwrappedType == typeof(char) &&
				(leftUnwrappedType == typeof(short) || leftUnwrappedType == typeof(int) || leftUnwrappedType == typeof(long) || leftUnwrappedType == typeof(double) || leftUnwrappedType == typeof(decimal)))
			{
				right = Expression.Convert(right, left.Type);
			}
			else if (rightUnwrappedType == typeof(short) &&
				(leftUnwrappedType == typeof(int) || leftUnwrappedType == typeof(long) || leftUnwrappedType == typeof(double) || leftUnwrappedType == typeof(decimal)))
			{
				right = Expression.Convert(right, left.Type);
			}
			else if (rightUnwrappedType == typeof(int) &&
				(leftUnwrappedType == typeof(long) || leftUnwrappedType == typeof(double) || leftUnwrappedType == typeof(decimal)))
			{
				right = Expression.Convert(right, left.Type);
			}
			else if (rightUnwrappedType == typeof(long) &&
				(leftUnwrappedType == typeof(double) || leftUnwrappedType == typeof(decimal)))
			{
				right = Expression.Convert(right, left.Type);
			}
		}

		protected Expression ParseAddOrSubtract()
		{
			var leftOperand = ParseMultiplyOrDivide();
			var retval = leftOperand;

			while (this.token == ComputedExpressionToken.Add || this.token == ComputedExpressionToken.Subtract)
			{
				var operationToken = this.token;

				Consume();

				var rightOperand = ParseMultiplyOrDivide();

				NormalizeOperandsForArithmatic(ref leftOperand, ref rightOperand);

				if (operationToken == ComputedExpressionToken.Add)
				{
					retval = Expression.Add(leftOperand, rightOperand);
				}
				else if (operationToken == ComputedExpressionToken.Subtract)
				{
					retval = Expression.Subtract(leftOperand, rightOperand);
				}

				Consume();
			}
			
			return retval;
		}

		protected Expression ParseMultiplyOrDivide()
		{
			var leftOperand = ParseUnary();
			var retval = leftOperand;

			while (this.token == ComputedExpressionToken.Multiply || this.token == ComputedExpressionToken.Divide || this.token == ComputedExpressionToken.Modulo)
			{
				var operationToken = this.token;

				Consume();

				var rightOperand = ParseUnary();

				NormalizeOperandsForArithmatic(ref leftOperand, ref rightOperand);

				switch (operationToken)
				{
				case ComputedExpressionToken.Multiply:
					retval = Expression.Multiply(leftOperand, rightOperand);
					break;
				case ComputedExpressionToken.Divide:
					retval = Expression.Divide(leftOperand, rightOperand);
					break;
				case ComputedExpressionToken.Modulo:
					retval = Expression.Modulo(leftOperand, rightOperand);
					break;
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
			Consume();
			var arguments = new List<Expression>();

			while (true)
			{
				if (this.token == ComputedExpressionToken.RightParen)
				{
					break;
				}

				var argument = ParseExpression();

				arguments.Add(argument);

				if (this.token == ComputedExpressionToken.Comma)
				{
					Consume();

					continue;
				}
				else if (this.token != ComputedExpressionToken.RightParen)
				{
					throw new InvalidOperationException("Expected ')'");
				}

				break;
			}

			Consume();

			if (target is ContainerExpression container)
			{
				var type = container.Type;

				var method = FindMatchingMethod(type, methodName, arguments.Select(c => c.Type).ToArray(), true);

				if (method == null)
				{
					throw new InvalidOperationException($"Unable to find static method named '{methodName}' on type {type}");
				}

				return Expression.Call(method, arguments.ToArray());
			}
			else
			{
				var method = FindMatchingMethod(target.Type, methodName, arguments.Select(c => c.Type).ToArray(), false);

				if (method == null)
				{
					throw new InvalidOperationException($"Unable to find method named '{methodName}'");
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
			if (this.referencedTypesByName.TryGetValue(name, out value))
			{
				return true;
			}

			var type = Type.GetType(name);

			if (type != null)
			{
				value = type;

				return true;
			}

			if (this.referencedAssemblies.Any(assembly => (type = assembly.GetType(name)) != null))
			{
				value = type;

				return true;
			}

			Assembly assembly2;

			try
			{
				//value = AppDomain.CurrentDomain.GetAssemblies().SelectMany(c => c.DefinedTypes).FirstOrDefault(c => c.FullName == name);

				foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
				{
					assembly2 = assembly;

					value = assembly.DefinedTypes.FirstOrDefault(c => c.FullName == name);

					if (value != null)
					{
						break;
					}
				}

				if (value != null)
				{
					return true;
				}

				value = null;

				return false;
			}
			catch (Exception e)
			{
				throw;
			}
		}

		private HashSet<string> namespaces;

		private bool DoesNamespaceResolve(string fullNamespace)
		{
			if (this.namespaces == null)
			{
				this.namespaces = new HashSet<string>();

				this.referencedTypesByName.Select(c => c.Value.Namespace).ForEach(c => this.namespaces.Add(c));
				try
				{
					AppDomain.CurrentDomain.GetAssemblies().SelectMany(c => c.DefinedTypes).ForEach(c => this.namespaces.Add(c.Namespace));
				}
				catch (Exception e)
				{
					throw;
				}
			}

			return this.namespaces.Contains(fullNamespace);
		}

		private bool DoesStaticMethodResolve(string identifier, out Type containingType)
		{
			var method = this.targetObject.Type.GetMethod(identifier, BindingFlags.Public | BindingFlags.Static);

			if (method != null)
			{
				containingType = this.targetObject.Type;

				return true;
			}

			var staticClasses = this.referencedTypesByName.Select(c => c.Value).Where(c => c.IsAbstract && c.IsSealed);

			var matchingMethods = staticClasses.SelectMany(c => c.GetMethods(BindingFlags.Static | BindingFlags.Public).Where(d => d.Name == identifier)).ToList();

			if (matchingMethods.Count == 0)
			{
				containingType = null;

				return false;
			}

			if (matchingMethods.Count > 1)
			{
				containingType = null;

				throw new InvalidOperationException($"Ambiguous match for method named '{identifier}'");
			}

			containingType = matchingMethods[0].DeclaringType;

			return true;
		}
		
		private class ContainerExpression
			: Expression
		{
			public string Namespace { get; }
			public override Type Type { get; }

			public ContainerExpression(string @namespace)
			{
				this.Namespace = @namespace;
			}

			public ContainerExpression(Type type)
			{
				this.Type = type;
			}

			public ContainerExpression Append(string identifier)
			{
				return new ContainerExpression(this.Namespace + "." + identifier);
			}
		}

		protected Expression ParseOperand()
		{
			if (this.token == ComputedExpressionToken.LeftParen)
			{
				Consume();

				var retval = ParseExpression();

				Expect(ComputedExpressionToken.RightParen);

				return retval;
			}

			Expression current = null;

			if (this.token == ComputedExpressionToken.Keyword && this.tokenizer.CurrentKeyword == ComputedExpressionKeyword.@this)
			{
				current = this.targetObject;
				Consume();

				if (this.token == ComputedExpressionToken.Period)
				{
					Consume();
				}
			}
			else if (this.token == ComputedExpressionToken.Keyword && this.tokenizer.CurrentKeyword == ComputedExpressionKeyword.@true)
			{
				Consume();

				return Expression.Constant(true);
			}
			else if (this.token == ComputedExpressionToken.Keyword && this.tokenizer.CurrentKeyword == ComputedExpressionKeyword.@false)
			{
				Consume();

				return Expression.Constant(false);
			}
			else if (this.token == ComputedExpressionToken.Keyword && this.tokenizer.CurrentKeyword == ComputedExpressionKeyword.@null)
			{
				Consume();

				return Expression.Constant(null);
			}

			if (this.token == ComputedExpressionToken.Identifier)
			{
				while (true)
				{
					var identifier = this.tokenizer.CurrentIdentifier;

					Consume();

					if (this.token == ComputedExpressionToken.LeftParen)
					{
						if (current is ContainerExpression containerExpression && containerExpression.Namespace != null)
						{
							throw new InvalidOperationException($"Namespace '{containerExpression.Namespace}' not expected");
						}

						// Calling a static method on a referenced type is supported as if the type was imported 'using static'

						if (current == null)
						{
							if (DoesStaticMethodResolve(identifier, out var type))
							{
								current = new ContainerExpression(type);
							}
							else
							{
								current = this.targetObject;
							}
						}

						current = ParseMethodCall(current, identifier);
					}
					else if (identifier == "value" && current == null)
					{
						current = Expression.Property(this.targetObject, this.propertyInfo);
					}
					else if (current is ContainerExpression container)
					{
						if (container.Type != null)
						{
							var member = current.Type.GetMember(identifier, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).FirstOrDefault();

							if (member is PropertyInfo || member is FieldInfo)
							{
								current = Expression.MakeMemberAccess(null, member);
							}
							else if (member is Type nestedType)
							{
								current = new ContainerExpression(nestedType);
							}
							else
							{
								throw new InvalidOperationException($"Unable to resolve '{identifier}' on type '{container.Type}'");
							}
						}
						else
						{
							var s = container.Namespace + "." + identifier;

							if (TryGetType(s, out var type))
							{
								current = new ContainerExpression(type);
							}
							else if (DoesNamespaceResolve(s))
							{
								current = container.Append(identifier);
							}
							else
							{
								TryGetType(s, out var type2);

								throw new InvalidOperationException($"Unable to resolve identifier '{s}'");
							}
						}
					}
					else if (current != null)
					{
						var member = current.Type.GetMember(identifier, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).SingleOrDefault();

						if (member is PropertyInfo || member is FieldInfo)
						{
							current = Expression.MakeMemberAccess(current, member);
						}
						else if (member is Type nestedType)
						{
							current = new ContainerExpression(nestedType);
						}
						else
						{
							throw new InvalidOperationException($"Unable to resolve property '{identifier}' on type '{current.Type}'");
						}
					}
					else
					{
						var member = this.targetObject.Type.GetMember(identifier, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).SingleOrDefault();

						if (member is PropertyInfo || member is FieldInfo)
						{
							current = Expression.MakeMemberAccess(this.targetObject, member);
						}
						else if (member is Type nestedType)
						{
							current = new ContainerExpression(nestedType);
						}
						else if (TryGetType(identifier, out var type))
						{
							current = new ContainerExpression(type);
						}
						else if (DoesNamespaceResolve(identifier))
						{
							current = new ContainerExpression(identifier);
						}
						else
						{
							throw new InvalidOperationException($"Unable to resolve identifier '{identifier}'");
						}
					}

					if (this.token == ComputedExpressionToken.Period)
					{
						Consume();

						if (this.token != ComputedExpressionToken.Identifier)
						{
							throw new InvalidOperationException();
						}

						continue;
					}

					return current;
				}
			}

			var multiplier = 1;

			if (this.token == ComputedExpressionToken.Subtract)
			{
				multiplier = -1;
			}
			
			switch (this.token)
			{
			case ComputedExpressionToken.IntegerLiteral:
				Consume();

				return Expression.Constant(multiplier * (int)this.tokenizer.CurrentInteger);
			case ComputedExpressionToken.LongLiteral:
				Consume();

				return Expression.Constant(multiplier * this.tokenizer.CurrentInteger);
			case ComputedExpressionToken.StringLiteral:
				Consume();

				return Expression.Constant(this.tokenizer.CurrentString);
			default:
				if (current != null)
				{
					return current;
				}
				break;
			}
			
			throw new InvalidOperationException();
		}
	}
}

