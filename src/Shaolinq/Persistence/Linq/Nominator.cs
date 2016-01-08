// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Shaolinq.Persistence.Linq.Expressions;

namespace Shaolinq.Persistence.Linq
{
	internal class Nominator
		: SqlExpressionVisitor
	{
		public readonly HashSet<Expression> candidates;
		protected Func<Expression, bool> canBeColumn;
		private readonly bool includeIntegralRootExpression;
		private Expression rootExpression;
		private bool inProjection;

		internal Nominator(Func<Expression, bool> canBeColumn, bool includeIntegralRootExpression = false)
		{
			this.canBeColumn = canBeColumn;
			this.includeIntegralRootExpression = includeIntegralRootExpression;
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

		public virtual HashSet<Expression> Nominate(Expression expression)
		{
			if (this.includeIntegralRootExpression)
			{
				this.rootExpression = expression;
			}

			this.Visit(expression);

			return this.candidates;
		}

		protected override Expression Visit(Expression expression)
		{
			if (expression == null)
			{
				return null;
			}

			if (expression.NodeType != (ExpressionType)SqlExpressionType.Subquery)
			{
				base.Visit(expression);
			}
			
			if (this.canBeColumn(expression)
				|| (expression.Type.IsIntegralType() && expression == rootExpression))
			{
				this.candidates.Add(expression);
			}
			
			return expression;
		}

		protected override Expression VisitProjection(SqlProjectionExpression projection)
		{
			var saveInProjection = this.inProjection;
			this.inProjection = true;
			var retval = base.VisitProjection(projection);
			this.inProjection = saveInProjection;
			return retval;
		}

		protected override Expression VisitJoin(SqlJoinExpression join)
		{
			var left = this.Visit(join.Left);
			var right = this.Visit(join.Right);
			var condition = join.JoinCondition;

			if (this.inProjection)
			{
				var saveCanBeColumn = this.canBeColumn;

				this.canBeColumn = c => c is SqlColumnExpression;

				condition = this.Visit(join.JoinCondition);

				this.canBeColumn = saveCanBeColumn;
			}

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
			var where = selectExpression.Where;

			if (inProjection)
			{
				var saveCanBeColumn = this.canBeColumn;

				this.canBeColumn = c => c is SqlColumnExpression;

				where = this.Visit(selectExpression.Where);

				this.canBeColumn = saveCanBeColumn;
			}

			if (from != selectExpression.From || columns != selectExpression.Columns || orderBy != selectExpression.OrderBy || groupBy != selectExpression.GroupBy || take != selectExpression.Take || skip != selectExpression.Skip || where != selectExpression.Where)
			{
				return new SqlSelectExpression(selectExpression.Type, selectExpression.Alias, columns, from, where, orderBy, groupBy, selectExpression.Distinct, skip, take, selectExpression.ForUpdate);
			}

			return selectExpression;
		}
	}
}