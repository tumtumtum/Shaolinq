using System;
using Platform;
using System.Linq.Expressions;
using Shaolinq.Persistence.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Optimizers
{
	public class EnumTypeNormalizer
		: SqlExpressionVisitor
	{
		private readonly DataAccessModel dataAccessModel;
		public Type PersistedType { get; private set; }

		public EnumTypeNormalizer(DataAccessModel dataAccessModel, Type persistedType)
		{
			this.dataAccessModel = dataAccessModel;
			this.PersistedType = persistedType;
		}

		public static Expression Normalize(Expression expression, DataAccessModel dataAccessModel, Type persistedType)
		{
			var normalizer = new EnumTypeNormalizer(dataAccessModel, persistedType);

			return normalizer.Visit(expression);
		}

		protected override Expression VisitUnary(UnaryExpression unaryExpression)
		{
			var operand = unaryExpression.Operand;

			if (this.PersistedType == typeof(string)
				&& unaryExpression.NodeType == ExpressionType.Convert
				&& unaryExpression.Type.IsIntegerType(true))
			{
				if (operand.NodeType == (ExpressionType)SqlExpressionType.Column)
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

			if (left.Type.IsEnum)
			{
				if (!right.Type.IsEnum)
				{
					var lambda = Expression.Lambda(Expression.Convert(Expression.Call(null, MethodInfoFastRef.EnumToObjectMethod, Expression.Constant(left.Type), right), left.Type));

					right = lambda.Body;
				}
			}
			else if (right.Type.IsEnum)
			{
				if (!left.Type.IsEnum)
				{
					left = Expression.Convert(Expression.Call(null, MethodInfoFastRef.EnumToObjectMethod, Expression.Constant(right.Type), left), right.Type);
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
