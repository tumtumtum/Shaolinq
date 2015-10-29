using System;
using System.Linq.Expressions;
using System.Reflection;
using Platform;

namespace Shaolinq.Persistence.Linq.Expressions
{
	public class ExpressionInterpreter
	{
		protected static object interpretFailed = new object();

		public static object Interpret(Expression expression)
		{
			var interpreter = new ExpressionInterpreter();

			var result = interpreter.Visit(expression);
			
			if (result == interpretFailed)
			{
				result = ExpressionFastCompiler.CompileAndRun(expression);
			}

			return result;
		}

		protected object Visit(Expression expression)
		{
			switch (expression.NodeType)
			{
			case ExpressionType.Convert:
				return this.Visit((UnaryExpression)expression);
			case ExpressionType.MemberAccess:
				return this.Visit((MemberExpression)expression);
			case ExpressionType.Add:
			case ExpressionType.AndAlso:
			case ExpressionType.Or:
			case ExpressionType.OrElse:
				return this.Visit((BinaryExpression)expression);
			case ExpressionType.Call:
				return this.Visit((MethodCallExpression)expression);
			case ExpressionType.Constant:
				return this.Visit((ConstantExpression)expression);
			}

			return interpretFailed;
		}

		protected object Visit(ConstantExpression expression)
		{
			return expression.Value;
		}

		protected object Visit(MethodCallExpression expression)
		{
			var args = new object[expression.Arguments.Count];
			var parentValue = expression.Object != null ? this.Visit(expression.Object) : null;

			if (parentValue == interpretFailed)
			{
				return interpretFailed;
			}

			var i = 0;

			foreach (var arg in expression.Arguments)
			{
				var reflected = this.Visit(arg);

				if (reflected == interpretFailed)
				{
					return interpretFailed;
				}

				args[i++] = reflected;
			}

			return expression.Method.Invoke(parentValue, args);
		}

		protected object Visit(BinaryExpression expression)
		{
			switch (expression.NodeType)
			{
			case ExpressionType.Add:
				if (expression.Type == typeof(string))
				{
					var left = this.Visit(expression.Left);

					if (left == interpretFailed)
					{
						return interpretFailed;
					}

					var right = this.Visit(expression.Right);

					if (right == interpretFailed)
					{
						return interpretFailed;
					}

					return string.Concat(left, right);
				}
				return interpretFailed;
			default:
				return interpretFailed;
			}
		}

		protected object Visit(MemberExpression expression)
		{
			var parentValue = expression.Expression != null ? this.Visit(expression.Expression) : null;

			if (parentValue == interpretFailed)
			{
				return expression;
			}

			var fieldInfo = expression.Member as FieldInfo;

			if (fieldInfo != null)
			{
				return fieldInfo.GetValue(parentValue);
			}

			var propertyInfo = expression.Member as PropertyInfo;

			if (propertyInfo != null)
			{
				return propertyInfo.GetValue(parentValue, null);
			}

			return interpretFailed;
		}

		protected object Visit(UnaryExpression expression)
		{
			if (expression.Method != null)
			{
				return interpretFailed;
			}

			if (expression.NodeType != ExpressionType.Convert)
			{
				return interpretFailed;
			}

			var result = this.Visit(expression.Operand);

			if (result == interpretFailed)
			{
				return interpretFailed;
			}

			var underlyingType = expression.Type.GetUnwrappedNullableType();

			if (expression.Type == underlyingType)
			{
				return result;
			}

			return Convert.ChangeType(result, underlyingType);
		}
	}
}
