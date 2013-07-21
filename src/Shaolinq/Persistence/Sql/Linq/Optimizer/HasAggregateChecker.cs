using System.Linq.Expressions;
using Shaolinq.Persistence.Sql.Linq.Expressions;

namespace Shaolinq.Persistence.Sql.Linq.Optimizer
{
	/// <summary>
	/// Determines if a Select contains any aggregate expressions
	/// </summary>
	public class HasAggregateChecker
		: SqlExpressionVisitor
	{
		private bool hasAggregate = false;
		private readonly bool ignoreInSubqueries = true;

		private HasAggregateChecker(bool ignoreInSubqueries)
		{
			this.ignoreInSubqueries = ignoreInSubqueries;
		}

		internal static bool HasAggregates(SqlSelectExpression expression)
		{
			return HasAggregates(expression, true);
		}

		internal static bool HasAggregates(SqlSelectExpression expression, bool ignoreInSubqueries)
		{
			var checker = new HasAggregateChecker(ignoreInSubqueries);

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
			if (hasAggregate)
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

			this.Visit(select.Where);

			if (this.hasAggregate)
			{
				return select;
			}
			
			this.VisitOrderBy(select.OrderBy);

			if (this.hasAggregate)
			{
				return select;
			}

			this.VisitColumnDeclarations(select.Columns);

			return select;
		}

		protected override Expression VisitSubquery(SqlSubqueryExpression subquery)
		{
			// Ignore aggregates in sub queries

			if (this.hasAggregate || ignoreInSubqueries)
			{
				return subquery;
			}

			return Visit(subquery);
		}
	}
}
