// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Linq.Expressions;
using Platform;
using Shaolinq.Persistence.Linq.Expressions;
using Shaolinq.TypeBuilding;

namespace Shaolinq.Persistence.Linq.Optimizers
{
	public class EnumTypeNormalizer
		: SqlExpressionVisitor
	{
		public Type PersistedType { get; }

		public EnumTypeNormalizer(Type persistedType)
		{
			this.PersistedType = persistedType;
		}

		public static Expression Normalize(Expression expression, Type persistedType)
		{
			var normalizer = new EnumTypeNormalizer(persistedType);

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

		protected override Expression VisitBinary(BinaryExpression binaryExpression)
		{
			var left = this.Visit(binaryExpression.Left);
			var right = this.Visit(binaryExpression.Right);

			if (left.Type.GetUnwrappedNullableType().IsEnum)
			{
				if (!right.Type.GetUnwrappedNullableType().IsEnum)
				{
					right = Expression.Convert(Expression.Call(null, MethodInfoFastRef.EnumToObjectMethod, Expression.Constant(left.Type.GetUnwrappedNullableType()), Expression.Convert(right, typeof(int))), left.Type);
				}
			}
			else if (right.Type.GetUnwrappedNullableType().IsEnum)
			{
				if (!left.Type.GetUnwrappedNullableType().IsEnum)
				{
					left = Expression.Convert(Expression.Call(null, MethodInfoFastRef.EnumToObjectMethod, Expression.Constant(right.Type.GetUnwrappedNullableType()), Expression.Convert(left, typeof(int))), right.Type);
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
