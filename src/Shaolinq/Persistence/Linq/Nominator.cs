using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Shaolinq.Persistence.Linq.Expressions;

namespace Shaolinq.Persistence.Linq
{
	public class Nominator
		: SqlExpressionVisitor
	{
		public static readonly Nominator Default = new Nominator(CanBeColumn);
		public static readonly Nominator NominatorIncludingIntegralTypes = new Nominator(CanBeColumnIncludingIntegral);

		public readonly HashSet<Expression> candidates;
		protected readonly Func<Expression, bool> canBeColumn;

		public Nominator(Func<Expression, bool> canBeColumn)
		{
			this.canBeColumn = canBeColumn;
			this.candidates = new HashSet<Expression>();
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
			case (ExpressionType)SqlExpressionType.Subquery:
				return true;
			default:
				return false;
			}
		}

		public static bool CanBeColumnIncludingIntegral(Expression expression)
		{
			switch (expression.NodeType)
			{
				case (ExpressionType)SqlExpressionType.Column:
				case (ExpressionType)SqlExpressionType.Scalar:
				case (ExpressionType)SqlExpressionType.FunctionCall:
				case (ExpressionType)SqlExpressionType.AggregateSubquery:
				case (ExpressionType)SqlExpressionType.Aggregate:
				case (ExpressionType)SqlExpressionType.Subquery:
					return true;
				default:
					return expression.Type.IsIntegralType();
			}
		}

		public virtual HashSet<Expression> Nominate(Expression expression)
		{
			this.Visit(expression);

			return this.candidates;
		}

		protected override Expression Visit(Expression expression)
		{
			if (expression != null)
			{
				if (expression.NodeType != (ExpressionType)SqlExpressionType.Subquery)
				{
					base.Visit(expression);
				}

				if (this.canBeColumn(expression))
				{
					this.candidates.Add(expression);
				}
			}

			return expression;
		}
		
		protected override Expression VisitJoin(SqlJoinExpression join)
		{
			var left = this.Visit(join.Left);
			var right = this.Visit(join.Right);

			var condition = join.JoinCondition;

			if (left != join.Left || right != join.Right || condition != join.JoinCondition)
			{
				return new SqlJoinExpression(join.Type, join.JoinType, left, right, condition);
			}

			return join;
		}

		protected override Expression VisitSelect(SqlSelectExpression selectExpression)
		{
			var from = this.VisitSource(selectExpression.From);

			var orderBy = selectExpression.OrderBy;
			var groupBy = selectExpression.GroupBy;
			var skip = selectExpression.Skip;
			var take = selectExpression.Take;
			var columns = this.VisitColumnDeclarations(selectExpression.Columns);

			if (from != selectExpression.From || columns != selectExpression.Columns || orderBy != selectExpression.OrderBy || groupBy != selectExpression.GroupBy || take != selectExpression.Take || skip != selectExpression.Skip)
			{
				return new SqlSelectExpression(selectExpression.Type, selectExpression.Alias, columns, from, selectExpression.Where, orderBy, groupBy, selectExpression.Distinct, skip, take, selectExpression.ForUpdate);
			}

			return selectExpression;
		}
	}
}