// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)

using System.Linq.Expressions;
using Shaolinq.Persistence.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Optimizers
{
	/// <summary>
	/// Determines if a Select contains any aggregate expressions
	/// </summary>
	public class SqlAggregateChecker
		: SqlExpressionVisitor
	{
		private bool hasAggregate = false;
		private readonly bool ignoreInSubqueries = true;

		private SqlAggregateChecker(bool ignoreInSubqueries)
		{
			this.ignoreInSubqueries = ignoreInSubqueries;
		}

		internal static bool HasAggregates(SqlSelectExpression expression)
		{
			return HasAggregates(expression, true);
		}

		internal static bool HasAggregates(SqlSelectExpression expression, bool ignoreInSubqueries)
		{
			var checker = new SqlAggregateChecker(ignoreInSubqueries);

			checker.Visit(expression);

			return checker.hasAggregate;
		}

		protected override Expression VisitJoin(SqlJoinExpression join)
		{
			if (this.hasAggregate)
			{
				return join;
			}

			return base.VisitJoin(join);
		}

		protected override Expression VisitAggregate(SqlAggregateExpression sqlAggregate)
		{
			this.hasAggregate = true;

			return sqlAggregate;
		}

		protected override Expression VisitProjection(SqlProjectionExpression projection)
		{
			if (this.hasAggregate)
			{
				return projection;
			}

			return base.VisitProjection(projection);
		}

		protected override Expression VisitSelect(SqlSelectExpression select)
		{
			// Only consider aggregates in these locations

			if (this.hasAggregate)
			{
				return select;
			}

			Visit(select.Where);

			if (this.hasAggregate)
			{
				return select;
			}
			
			VisitExpressionList(select.OrderBy);

			if (this.hasAggregate)
			{
				return select;
			}

			VisitColumnDeclarations(select.Columns);

			return select;
		}

		protected override Expression VisitSubquery(SqlSubqueryExpression subquery)
		{
			// Ignore aggregates in sub queries

			if (this.hasAggregate || this.ignoreInSubqueries)
			{
				return subquery;
			}

			return Visit(subquery);
		}
	}
}
