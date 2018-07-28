// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)

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
			case ExpressionType.Conditional:
				return expression.Type.IsIntegralType();
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

			Visit(expression);

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
				|| (expression.Type.IsIntegralType() && expression == this.rootExpression))
			{
				this.candidates.Add(expression);
			}
			
			return expression;
		}

		protected override Expression VisitProjection(SqlProjectionExpression projection)
		{
			var saveInProjection = this.inProjection;
			this.inProjection = true;
			base.VisitProjection(projection);
			this.inProjection = saveInProjection;
			return projection;
		}

		protected override Expression VisitJoin(SqlJoinExpression join)
		{
			Visit(join.Left);
			Visit(join.Right);
			
			if (this.inProjection)
			{
				var saveCanBeColumn = this.canBeColumn;

				this.canBeColumn = c => c is SqlColumnExpression;

				Visit(join.JoinCondition);

				this.canBeColumn = saveCanBeColumn;
			}

			return join;
		}

		protected override Expression VisitSelect(SqlSelectExpression selectExpression)
		{
			VisitSource(selectExpression.From);
			VisitColumnDeclarations(selectExpression.Columns);

			if (this.inProjection)
			{
				var saveCanBeColumn = this.canBeColumn;

				this.canBeColumn = c => c is SqlColumnExpression;

				VisitExpressionList(selectExpression.OrderBy);
				VisitExpressionList(selectExpression.GroupBy);
				Visit(selectExpression.Skip);
				Visit(selectExpression.Take);
				Visit(selectExpression.Where);

				this.canBeColumn = saveCanBeColumn;
			}

			return selectExpression;
		}
	}
}