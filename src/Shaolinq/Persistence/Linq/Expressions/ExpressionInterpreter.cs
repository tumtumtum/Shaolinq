// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using Platform;

namespace Shaolinq.Persistence.Linq.Expressions
{
	public class ExpressionInterpreter
	{
		protected static readonly object InterpretFailed = new object();

		public static object Interpret(Expression expression)
		{
			var interpreter = new ExpressionInterpreter();

			var result = interpreter.Visit(expression);
			
			if (result == InterpretFailed)
			{
				result = ExpressionFastCompiler.CompileAndRun(expression);
			}

			return result;
		}

		protected object Visit(Expression expression)
		{
			switch (expression.NodeType)
			{
			case ExpressionType.New:
				return this.Visit((NewExpression)expression);
			case ExpressionType.MemberInit:
				return this.Visit((MemberInitExpression)expression);
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
			case ExpressionType.Equal:
				return this.Visit((BinaryExpression)expression);
			}

			return InterpretFailed;
		}

		protected object Visit(ConstantExpression expression)
		{
			return expression.Value;
		}

		protected object Visit(MemberInitExpression expression)
		{
			var obj = this.Visit(expression.NewExpression);

			if (obj == InterpretFailed)
			{
				return obj;
			}

			foreach (var binding in expression.Bindings)
			{
				var value = this.Visit(obj, binding);

				if (value == InterpretFailed)
				{
					return value;
				}
			}

			return obj;
		}

		protected object Visit(object obj, MemberBinding binding)
		{
			switch (binding.BindingType)
			{
			case MemberBindingType.Assignment:
				var assignment = ((MemberAssignment)binding);

				var value = this.Visit(assignment.Expression);

				if (value == InterpretFailed)
				{
					return value;
				}

				var fieldInfo = assignment.Member as FieldInfo;

				if (fieldInfo != null)
				{
					fieldInfo.SetValue(obj, value);

					return binding;
				}

				var propertyInfo = assignment.Member as PropertyInfo;

				if (propertyInfo != null)
				{
					propertyInfo.SetValue(obj, value, null);

					return binding;
				}

				return InterpretFailed;
			}

			return InterpretFailed;
		}


		protected object Visit(NewExpression expression)
		{
			// ReSharper disable once ConditionIsAlwaysTrueOrFalse

			if (expression.Constructor == null)
			{
				return Activator.CreateInstance(expression.Type);
			}

			var args = new object[expression.Arguments.Count];
			
			var i = 0;

			foreach (var arg in expression.Arguments)
			{
				var reflected = this.Visit(arg);

				if (reflected == InterpretFailed)
				{
					return reflected;
				}

				args[i++] = reflected;
			}

			return expression.Constructor.Invoke(args);
		}

		protected object Visit(MethodCallExpression expression)
		{
			var args = new object[expression.Arguments.Count];
			var parentValue = expression.Object != null ? this.Visit(expression.Object) : null;

			if (parentValue == InterpretFailed)
			{
				return InterpretFailed;
			}

			var i = 0;

			foreach (var arg in expression.Arguments)
			{
				var reflected = this.Visit(arg);

				if (reflected == InterpretFailed)
				{
					return InterpretFailed;
				}

				args[i++] = reflected;
			}

			return expression.Method.Invoke(parentValue, args);
		}

		protected object Visit(BinaryExpression expression)
		{
			switch (expression.NodeType)
			{
			case ExpressionType.Equal:
			case ExpressionType.NotEqual:
			{
				if (expression.Type == typeof(bool))
				{
					var left = this.Visit(expression.Left);

					if (left == InterpretFailed)
					{
						return InterpretFailed;
					}

					var right = this.Visit(expression.Right);

					if (right == InterpretFailed)
					{
						return InterpretFailed;
					}

					if (left == right)
					{
						return expression.NodeType == ExpressionType.Equal;
					}

					if (left is short && right is short)
					{
						return ((short)left == (short)right) && expression.NodeType == ExpressionType.Equal;
					}
					else if (left is int && right is int)
					{
						return (int)left == (int)right && expression.NodeType == ExpressionType.Equal;
					}
					else if (left is long && right is long)
					{
						return (long)left == (long)right && expression.NodeType == ExpressionType.Equal;
					}
					else if (left is string && right is string)
					{
						return (string)left == (string)right && expression.NodeType == ExpressionType.Equal;
					}
					else if (expression.Left.Type == typeof(object) && expression.Right.Type == typeof(object))
					{
						return EqualityComparer<object>.Default.Equals(left, right);
					}

					return InterpretFailed;
				}

				return InterpretFailed;
			}
			case ExpressionType.Add:
			{
				var left = this.Visit(expression.Left);

				if (left == InterpretFailed)
				{
					return InterpretFailed;
				}

				var right = this.Visit(expression.Right);

				if (right == InterpretFailed)
				{
					return InterpretFailed;
				}

				if (left is short && right is short)
				{
					return ((short)left + (short)right);
				}
				else if (left is int && right is int)
				{
					return (int)left + (int)right;
				}
				else if (left is long && right is long)
				{
					return (long)left + (long)right;
				}
				else if (left is string || right is string)
				{
					return Convert.ToString(left)  + Convert.ToString(right);
				}

				return InterpretFailed;
			}
			default:
				return InterpretFailed;
			}
		}

		protected object Visit(MemberExpression expression)
		{
			var parentValue = expression.Expression != null ? this.Visit(expression.Expression) : null;

			if (parentValue == InterpretFailed)
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

			return InterpretFailed;
		}

		protected object Visit(UnaryExpression expression)
		{
			if (expression.Method != null)
			{
				return InterpretFailed;
			}

			if (expression.NodeType != ExpressionType.Convert)
			{
				return InterpretFailed;
			}

			var result = this.Visit(expression.Operand);

			if (result == InterpretFailed)
			{
				return InterpretFailed;
			}

			if (result == null)
			{
				return Expression.Constant(null, expression.Type);
			}

			var underlyingType = expression.Operand.Type.GetUnwrappedNullableType();

			if (expression.Type == underlyingType)
			{
				return result;
			}

			try
			{
				return Convert.ChangeType(result, expression.Type);
			}
			catch (InvalidCastException)
			{
				return InterpretFailed;
			}
		}
	}
}
