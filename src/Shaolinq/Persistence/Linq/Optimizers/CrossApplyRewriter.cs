using System.Linq.Expressions;
using Shaolinq.Persistence.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Optimizers
{
	public class SqlSqlCrossApplyRewriter : SqlExpressionVisitor
	{
		private SqlSqlCrossApplyRewriter()
		{
		}

		public static Expression Rewrite(Expression expression)
		{
			return new SqlSqlCrossApplyRewriter().Visit(expression);
		}

		protected override Expression VisitJoin(SqlJoinExpression join)
		{
			join = (SqlJoinExpression)base.VisitJoin(join);

			if (join.JoinType == SqlJoinType.CrossApply || join.JoinType == SqlJoinType.OuterApply)
			{
				if (join.Right is SqlTableExpression)
				{
					return new SqlJoinExpression(join.Type, SqlJoinType.Cross, join.Left, join.Right, null);
				}
				else
				{
					var select = join.Right as SqlSelectExpression;

					// Only consider rewriting cross apply if 
					//   1) right side is a select
					//   2) other than in the where clause in the right-side select, no left-side declared aliases are referenced
					//   3) and has no behavior that would change semantics if the where clause is removed (like groups, aggregates, take, skip, etc).
					// Note: it is best to attempt this after redundant subqueries have been removed.
					if (select != null
						&& select.Take == null
						&& select.Skip == null
						&& !SqlAggregateChecker.HasAggregates(select)
						&& (select.GroupBy == null || select.GroupBy.Count == 0))
					{
						var selectWithoutWhere = select.ChangeWhere(null);
						var referencedAliases = SqlReferencedAliasGatherer.Gather(selectWithoutWhere);
						var declaredAliases = SqlDeclaredAliasGatherer.Gather(join.Left);

						referencedAliases.IntersectWith(declaredAliases);

						if (referencedAliases.Count == 0)
						{
							var where = select.Where;

							select = selectWithoutWhere;

							var pc = ColumnProjector.ProjectColumns(new NormalNominator(CanBeColumn), where, select.Columns, select.Alias, SqlDeclaredAliasGatherer.Gather(select.From));

							select = select.ChangeColumns(pc.Columns);
							where = pc.Projector;

							var joinType = (where == null) ? SqlJoinType.Cross : (join.JoinType == SqlJoinType.CrossApply ? SqlJoinType.Inner : SqlJoinType.Left);

							return new SqlJoinExpression(typeof(void), joinType, join.Left, select, where);
						}
					}
				}
			}

			return join;
		}

		public static bool CanBeColumn(Expression expression)
		{
			switch (expression.NodeType)
			{
			case (ExpressionType)SqlExpressionType.Column:
			case (ExpressionType)SqlExpressionType.Scalar:
			case (ExpressionType)SqlExpressionType.FunctionCall:
			case (ExpressionType)SqlExpressionType.AggregateSubquery:
			case (ExpressionType)SqlExpressionType.Aggregate:
				return true;
			default:
				return false;
			}
		}
	}
}
