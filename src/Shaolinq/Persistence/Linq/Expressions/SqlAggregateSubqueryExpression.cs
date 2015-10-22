// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Expressions
{
	public class SqlAggregateSubqueryExpression
		: SqlBaseExpression
	{
		public String GroupByAlias { get; }
		public Expression AggregateInGroupSelect { get; }
		public SqlSubqueryExpression AggregateAsSubquery { get; set; }
		public override ExpressionType NodeType => (ExpressionType)SqlExpressionType.AggregateSubquery;

		public SqlAggregateSubqueryExpression(string groupByAlias, Expression aggregateInGroupSelect, SqlSubqueryExpression aggregateAsSubquery)
			: base(aggregateAsSubquery.Type)
		{
			this.AggregateInGroupSelect = aggregateInGroupSelect;
			this.GroupByAlias = groupByAlias;
			this.AggregateAsSubquery = aggregateAsSubquery;
		}
	}
}
