// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)

using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Shaolinq.Persistence.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Optimizers
{
	public class SqlCrossJoinRewriter
		: SqlExpressionVisitor
	{
		private Expression currentWhere;

		public static Expression Rewrite(Expression expression)
		{
			return new SqlCrossJoinRewriter().Visit(expression);
		}

		protected override Expression VisitSelect(SqlSelectExpression select)
		{
			var savedWhere = this.currentWhere;

			try
			{
				this.currentWhere = select.Where;

				var result = (SqlSelectExpression)base.VisitSelect(select);

				if (this.currentWhere != result.Where)
				{
					return result.ChangeWhere(this.currentWhere);
				}

				return result;
			}
			finally
			{
				this.currentWhere = savedWhere;
			}
		}
		
		protected SqlJoinExpression UpdateJoin(SqlJoinExpression join, SqlJoinType joinType, Expression left, Expression right, Expression condition)
		{
			if (joinType != join.JoinType || left != join.Left || right != join.Right || condition != join.JoinCondition)
			{
				return new SqlJoinExpression(join.Type, joinType, left, right, condition);
			}

			return join;
		}

		protected override Expression VisitJoin(SqlJoinExpression join)
		{
			join = (SqlJoinExpression)base.VisitJoin(join);

			if (join.JoinType == SqlJoinType.Cross && this.currentWhere != null)
			{
				var declaredLeft = SqlDeclaredAliasGatherer.Gather(join.Left);
				var declaredRight = SqlDeclaredAliasGatherer.Gather(join.Right);
				var declared = new HashSet<string>(declaredLeft.Union(declaredRight));
				var exprs = this.currentWhere.Split(ExpressionType.And, ExpressionType.AndAlso);
				var good = exprs.Where(e => CanBeJoinCondition(e, declaredLeft, declaredRight, declared)).ToList();

				if (good.Count > 0 )
				{
					var condition = good.Join(ExpressionType.And);

					join = UpdateJoin(join, SqlJoinType.Inner, join.Left, join.Right, condition);

					var newWhere = exprs.Where(e => !good.Contains(e)).Join(ExpressionType.And);
					this.currentWhere = newWhere;
				}
			}

			return join;
		}

		private static bool CanBeJoinCondition(Expression expression, IEnumerable<string> left, IEnumerable<string> right, IEnumerable<string> all)
		{
			var referenced = SqlReferencedAliasGatherer.Gather(expression);
			
			var leftOkay = referenced.Intersect(left).Any();
			var rightOkay = referenced.Intersect(right).Any();

			var subset = referenced.IsSubsetOf(all);

			return leftOkay && rightOkay && subset;
		}
	}
}
