// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

using System.Linq;
using System.Linq.Expressions;
using Shaolinq.Persistence.Linq.Expressions;

namespace Shaolinq.Postgres.Shared
{
	public class PostgresSharedDataDefinitionExpressionAmmender
		: SqlExpressionVisitor
	{
		private bool foundPrimaryKeyAutoIncrementColumnConstraint;

		public static Expression Ammend(Expression expression)
		{
			var processor = new PostgresSharedDataDefinitionExpressionAmmender();

			return processor.Visit(expression);
		}
		
		protected override Expression VisitSimpleConstraint(SqlSimpleConstraintExpression simpleConstraintExpression)
		{
			if (simpleConstraintExpression.Constraint == SqlSimpleConstraint.PrimaryKeyAutoIncrement)
			{
				this.foundPrimaryKeyAutoIncrementColumnConstraint = true;
			}

			return base.VisitSimpleConstraint(simpleConstraintExpression);
		}

		protected override Expression VisitColumnDefinition(SqlColumnDefinitionExpression columnDefinitionExpression)
		{
			this.foundPrimaryKeyAutoIncrementColumnConstraint = false;

			var retval = (SqlColumnDefinitionExpression)base.VisitColumnDefinition(columnDefinitionExpression);

			if (this.foundPrimaryKeyAutoIncrementColumnConstraint)
			{
				retval = new SqlColumnDefinitionExpression(retval.ColumnName, "SERIAL", retval.ConstraintExpressions);
			}

			return retval;
		}
	}
}
