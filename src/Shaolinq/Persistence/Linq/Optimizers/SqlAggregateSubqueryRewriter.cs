// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Shaolinq.Persistence.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Optimizers
{
	/// <summary>
	/// Rewrite aggregate expressions, moving them into same select expression that has the group-by clause.
	/// </summary>
	public class SqlAggregateSubqueryRewriter
		: SqlExpressionVisitor
	{
		private readonly ILookup<string, SqlAggregateSubqueryExpression> aggregateSubqueriesBySelectAlias;
		private readonly Dictionary<SqlAggregateSubqueryExpression, Expression> aggregateSubqueryInstances;

		private SqlAggregateSubqueryRewriter(Expression expr)
		{
			this.aggregateSubqueryInstances = new Dictionary<SqlAggregateSubqueryExpression, Expression>();
			this.aggregateSubqueriesBySelectAlias = AggregateSubqueryFinder.Find(expr).OfType<SqlAggregateSubqueryExpression>().ToLookup(a => a.GroupByAlias);
		}

		public static Expression Rewrite(Expression expr)
		{
			return new SqlAggregateSubqueryRewriter(expr).Visit(expr);
		}

		protected override Expression VisitSelect(SqlSelectExpression select)
		{
			select = (SqlSelectExpression)base.VisitSelect(select);

			if (this.aggregateSubqueriesBySelectAlias.Contains(select.Alias))
			{
				var columnsIncludingAggregates = new List<SqlColumnDeclaration>(select.Columns);

				foreach (var aggregateSubqueryExpression in this.aggregateSubqueriesBySelectAlias[select.Alias])
				{
					var name = "AGGR" + columnsIncludingAggregates.Count;

					var columnDeclaration = new SqlColumnDeclaration(name, aggregateSubqueryExpression.AggregateInGroupSelect);

					this.aggregateSubqueryInstances.Add(aggregateSubqueryExpression, new SqlColumnExpression(aggregateSubqueryExpression.Type, aggregateSubqueryExpression.GroupByAlias, name));

					columnsIncludingAggregates.Add(columnDeclaration);
				}

				return new SqlSelectExpression(select.Type, select.Alias, columnsIncludingAggregates.ToReadOnlyCollection(), select.From, select.Where, select.OrderBy, select.GroupBy, select.Distinct, select.Skip, select.Take, select.ForUpdate);
			}

			return select;
		}

		protected override Expression VisitAggregateSubquery(SqlAggregateSubqueryExpression aggregate)
		{
			Expression mapped;

			if (this.aggregateSubqueryInstances.TryGetValue(aggregate, out mapped))
			{
				return mapped;
			}

			return this.Visit(aggregate.AggregateAsSubquery);
		}
	}
}
