// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System.Linq.Expressions;
using Shaolinq.Persistence.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Optimizers
{
	public class SqlCrossApplyRewriter : SqlExpressionVisitor
	{
		private SqlCrossApplyRewriter()
		{
		}

		public static Expression Rewrite(Expression expression)
		{
			return new SqlCrossApplyRewriter().Visit(expression);
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
					
					if (select != null && select.Take == null && select.Skip == null && !SqlAggregateChecker.HasAggregates(select) && (select.GroupBy == null || select.GroupBy.Count == 0))
					{
						var selectWithoutWhere = select.ChangeWhere(null);
						var referencedAliases = SqlReferencedAliasGatherer.Gather(selectWithoutWhere);
						var declaredAliases = SqlDeclaredAliasGatherer.Gather(join.Left);

						referencedAliases.IntersectWith(declaredAliases);

						if (referencedAliases.Count == 0)
						{
							var where = select.Where;

							select = selectWithoutWhere;

							var pc = ColumnProjector.ProjectColumns(new Nominator(Nominator.CanBeColumn), where, select.Columns, select.Alias, SqlDeclaredAliasGatherer.Gather(select.From));

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
	}
}
