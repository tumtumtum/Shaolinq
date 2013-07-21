using System.Collections.Generic;
using System.Linq.Expressions;
using Shaolinq.Persistence.Sql.Linq.Expressions;

namespace Shaolinq.Persistence.Sql.Linq.Optimizer
{
	public class AggregateSubqueryMerger
		: SqlExpressionVisitor
	{
		private AggregateSubqueryMerger()
		{
		}

		public static Expression Merge(Expression expression)
		{
			var merger = new AggregateSubqueryMerger();
            
			return merger.Visit(expression);
		}

		protected override Expression VisitSelect(SqlSelectExpression selectExpression)
		{
			if (selectExpression.Columns.Count == 1 
				&& selectExpression.From.NodeType == (ExpressionType)SqlExpressionType.Select
				&& selectExpression.Columns[0].Expression.NodeType == (ExpressionType)SqlExpressionType.Aggregate)
			{
				var from = (SqlSelectExpression)selectExpression.From;
				var aggregateExpression = (SqlAggregateExpression)selectExpression.Columns[0].Expression;

				if (from.Columns.Count > 1
					|| aggregateExpression.IsDistinct
					|| from.Distinct
					|| from.Take != null
					|| from.Skip != null
					|| from.GroupBy != null
					/* Don't fold a from with an orderby into the outer select if it has a count or other aggregate */
					|| from.OrderBy != null && from.OrderBy.Count > 0 && HasAggregateChecker.HasAggregates(selectExpression))
				{
					return base.VisitSelect(selectExpression);
				}

				var newColumns = new List<SqlColumnDeclaration>();

				if (from.Columns.Count == 1)
				{
					foreach (var column in from.Columns)
					{
						if (column.Expression.NodeType != (ExpressionType)SqlExpressionType.Column)
						{
							return base.VisitSelect(selectExpression);
						}

						var sqlColumnExpression = (SqlColumnExpression)column.Expression;
                        
						var newAggregate = new SqlAggregateExpression
						(
							aggregateExpression.Type,
							aggregateExpression.AggregateType,
							column.Expression,
							aggregateExpression.IsDistinct
						);

						newColumns.Add(new SqlColumnDeclaration(column.Name, newAggregate));
					}
				}
				else
				{
					newColumns.Add(selectExpression.Columns[0]);
				}

				var where = Visit(from.Where);

				return new SqlSelectExpression(from.Type, from.Alias, newColumns, from.From, where, from.OrderBy, from.GroupBy, from.Distinct, from.Skip, from.Take, from.ForUpdate || selectExpression.ForUpdate);
			}

			return base.VisitSelect(selectExpression);
		}
	}
}
