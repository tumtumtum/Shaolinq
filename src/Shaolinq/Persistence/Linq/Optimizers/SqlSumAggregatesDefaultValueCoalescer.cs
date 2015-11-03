// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Linq.Expressions;
using Platform;
using Shaolinq.Persistence.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Optimizers
{
	public class SqlSumAggregatesDefaultValueCoalescer
		: SqlExpressionVisitor
	{
		private SqlProjectionExpression currentProjection;

		private SqlSumAggregatesDefaultValueCoalescer()
		{	
		}

		public static Expression Coalesce(Expression expression)
		{
			var coalescer = new SqlSumAggregatesDefaultValueCoalescer();

			return coalescer.Visit(expression);
		}

		protected override Expression VisitProjection(SqlProjectionExpression projection)
		{
			var previousProjection = this.currentProjection;

			this.currentProjection = projection;

			var retval = base.VisitProjection(projection);

			this.currentProjection = previousProjection;

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
