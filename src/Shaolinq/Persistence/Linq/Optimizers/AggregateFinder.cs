// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

﻿using System.Collections.Generic;
using System.Linq.Expressions;
using Shaolinq.Persistence.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Optimizers
{
	/// <summary>
	/// Finds and returns all aggregates within an expression.
	/// </summary>
	public class AggregateFinder
		: SqlExpressionVisitor
	{
		private readonly List<SqlAggregateSubqueryExpression> aggregatesFound;
		
		private AggregateFinder()
		{
			aggregatesFound = new List<SqlAggregateSubqueryExpression>();
		}

		public static List<SqlAggregateSubqueryExpression> Gather(Expression expression)
		{
			var gatherer = new AggregateFinder();

			gatherer.Visit(expression);

			return gatherer.aggregatesFound;
		}

		protected override Expression VisitAggregateSubquery(SqlAggregateSubqueryExpression aggregate)
		{
			this.aggregatesFound.Add(aggregate);

			return base.VisitAggregateSubquery(aggregate);
		}
	}
}
