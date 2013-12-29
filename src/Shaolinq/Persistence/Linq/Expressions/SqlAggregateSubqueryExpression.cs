// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

﻿using System;
using System.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Expressions
{
	public class SqlAggregateSubqueryExpression
		: SqlBaseExpression
	{
		public String GroupByAlias { get; private set; }
		public Expression AggregateInGroupSelect { get; private set; }
		public SqlSubqueryExpression AggregateAsSubquery { get; set; }

		public override ExpressionType NodeType
		{
			get
			{
				return (ExpressionType)SqlExpressionType.AggregateSubquery;
			}
		}

		public SqlAggregateSubqueryExpression(string groupByAlias, Expression aggregateInGroupSelect, SqlSubqueryExpression aggregateAsSubquery)
			: base(aggregateAsSubquery.Type)
		{
			this.AggregateInGroupSelect = aggregateInGroupSelect;
			this.GroupByAlias = groupByAlias;
			this.AggregateAsSubquery = aggregateAsSubquery;
		}
	}
}
