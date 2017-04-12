// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System.Linq.Expressions;
using Shaolinq.Persistence.Linq;
using Shaolinq.Persistence.Linq.Expressions;

namespace Shaolinq.MySql
{
	public class MySqlDefaultValueConstraintFixer
		: SqlExpressionVisitor
	{
		public static Expression Fix(Expression expression)
		{
			return new MySqlDefaultValueConstraintFixer().Visit(expression);
		}

		protected override Expression VisitConstraint(SqlConstraintExpression expression)
		{
			if (expression.ConstraintType == ConstraintType.DefaultValue && expression.ConstraintName != null)
			{
				return expression.ChangeConstraintName(null);
			}

			return base.VisitConstraint(expression);
		}
	}
}