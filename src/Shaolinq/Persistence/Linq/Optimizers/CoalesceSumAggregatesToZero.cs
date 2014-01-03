using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Platform;
using Shaolinq.Persistence.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Optimizers
{
	public class CoalesceSumAggregatesToZero
		: SqlExpressionVisitor
	{
		private SqlProjectionExpression currentProjection;

		private CoalesceSumAggregatesToZero()
		{	
		}

		public static Expression Coalesce(Expression expression)
		{
			var coalescer = new CoalesceSumAggregatesToZero();

			return coalescer.Visit(expression);
		}

		protected override Expression VisitProjection(SqlProjectionExpression projection)
		{
			var previousProjection = currentProjection;

			currentProjection = projection;

			var retval = base.VisitProjection(projection);

			currentProjection = previousProjection;

			return retval;
		}

		protected override Expression VisitAggregate(SqlAggregateExpression sqlAggregate)
		{
			if (this.currentProjection != null && sqlAggregate.AggregateType == SqlAggregateType.Sum && sqlAggregate.Type.IsNullableType())
			{
				var defaultValue = this.currentProjection.DefaultValueExpression ?? Expression.Constant(Nullable.GetUnderlyingType(sqlAggregate.Type).GetDefaultValue());

				return new SqlFunctionCallExpression(sqlAggregate.Type, SqlFunction.Coalesce, sqlAggregate, defaultValue);
			}

			return sqlAggregate;
		}
	}
}
