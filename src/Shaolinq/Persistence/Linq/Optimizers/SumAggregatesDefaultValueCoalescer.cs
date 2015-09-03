// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Linq.Expressions;
using Platform;
using Shaolinq.Persistence.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Optimizers
{
	public class SumAggregatesDefaultValueCoalescer
		: SqlExpressionVisitor
	{
		private SqlProjectionExpression currentProjection;

		private SumAggregatesDefaultValueCoalescer()
		{	
		}

		public static Expression Coalesce(Expression expression)
		{
			var coalescer = new SumAggregatesDefaultValueCoalescer();

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
