// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System.Collections.Generic;
using System.Linq.Expressions;
using Shaolinq.Persistence.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Optimizers
{
	public class SqlAggregateFinder
		: SqlExpressionVisitor
	{
		private readonly List<SqlAggregateExpression> aggregatesFound;
		
		private SqlAggregateFinder()
		{
			this.aggregatesFound = new List<SqlAggregateExpression>();
		}

		public static List<SqlAggregateExpression> Find(Expression expression)
		{
			var gatherer = new SqlAggregateFinder();

			gatherer.Visit(expression);

			return gatherer.aggregatesFound;
		}

		protected override Expression VisitAggregate(SqlAggregateExpression aggregate)
		{
			this.aggregatesFound.Add(aggregate);

			return base.VisitAggregate(aggregate);
		}
	}
}
