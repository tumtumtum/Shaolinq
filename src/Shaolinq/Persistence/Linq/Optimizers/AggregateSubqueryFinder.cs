// Copyright (c) 2007-2014 Thong Nguyen (tumtumtum@gmail.com)

﻿using System.Collections.Generic;
using System.Linq.Expressions;
using Shaolinq.Persistence.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Optimizers
{
	/// <summary>
	/// Finds and returns all aggregates within an expression.
	/// </summary>
	public class AggregateSubqueryFinder
		: SqlExpressionVisitor
	{
		private readonly List<SqlAggregateSubqueryExpression> aggregatesFound;
		
		private AggregateSubqueryFinder()
		{
			aggregatesFound = new List<SqlAggregateSubqueryExpression>();
		}

		public static List<SqlAggregateSubqueryExpression> Find(Expression expression)
		{
			var gatherer = new AggregateSubqueryFinder();

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
