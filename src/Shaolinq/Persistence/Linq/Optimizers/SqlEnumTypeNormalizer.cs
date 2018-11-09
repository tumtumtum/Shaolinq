// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Linq.Expressions;
using System.Reflection;
using Platform;
using Shaolinq.Persistence.Linq.Expressions;
using Shaolinq.TypeBuilding;

namespace Shaolinq.Persistence.Linq.Optimizers
{
	public class SqlEnumTypeNormalizer
		: SqlExpressionVisitor
	{
		private static readonly MethodInfo EnumToObjectMethod = TypeUtils.GetMethod(() => SqlEnumTypeNormalizer.EnumToObject(default, default));

		public Type PersistedType { get; }

		public SqlEnumTypeNormalizer(Type persistedType)
		{
			this.PersistedType = persistedType;
		}

		public static Expression Normalize(Expression expression, Type persistedType)
		{
			var normalizer = new SqlEnumTypeNormalizer(persistedType);

			return normalizer.Visit(expression);
		}

		protected override Expression VisitUnary(UnaryExpression unaryExpression)
		{
			var operand = unaryExpression.Operand;

			if (this.PersistedType == typeof(string)
				&& unaryExpression.NodeType == ExpressionType.Convert
				&& unaryExpression.Type.IsIntegerType(true))
			{
				if (operand.NodeType == (ExpressionType)SqlExpressionType.Column
					&& operand.Type.GetUnwrappedNullableType().IsEnum)
				{
					return operand;
				}
			}

			return base.VisitUnary(unaryExpression);
		}

		internal static object EnumToObject(Type enumType, object value)
		{
			if (value == null)
			{
				return null;
			}

			return Enum.ToObject(enumType.GetUnwrappedNullableType(), value);
		}

		protected override Expression VisitBinary(BinaryExpression binaryExpression)
		{
			var left = Visit(binaryExpression.Left);
			var right = Visit(binaryExpression.Right);

			if (left.Type.GetUnwrappedNullableType().IsEnum)
			{
				if (!right.Type.GetUnwrappedNullableType().IsEnum)
				{
					right = Expression.Convert(Expression.Call(EnumToObjectMethod, Expression.Constant(left.Type), Expression.Convert(right.StripConstantWrappers(), typeof(object))), left.Type);
				}
			}
			else if (right.Type.GetUnwrappedNullableType().IsEnum)
			{
				if (!left.Type.GetUnwrappedNullableType().IsEnum)
				{
					left = Expression.Convert(Expression.Call(EnumToObjectMethod, Expression.Constant(right.Type), Expression.Convert(left.StripConstantWrappers(), typeof(object))), right.Type);
				}
			}

			if (left != binaryExpression.Left || right != binaryExpression.Right)
			{
				return Expression.MakeBinary(binaryExpression.NodeType, left, right);
			}

			return binaryExpression;
		}
	}
}
