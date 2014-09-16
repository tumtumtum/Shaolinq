// Copyright (c) 2007-2014 Thong Nguyen (tumtumtum@gmail.com)

using System.Linq;
using System.Linq.Expressions;
using Shaolinq.Persistence;
using Shaolinq.Persistence.Linq.Expressions;

namespace Shaolinq.Sqlite
{
	public class SqliteDataDefinitionExpressionAmmender
		: SqlExpressionVisitor
	{
		private readonly SqlDataTypeProvider sqlDataTypeProvider;
		private bool currentIsPrimaryKey;

		private SqliteDataDefinitionExpressionAmmender(SqlDataTypeProvider sqlDataTypeProvider)
		{
			this.sqlDataTypeProvider = sqlDataTypeProvider;
		}

		public static Expression Ammend(Expression expression, SqlDataTypeProvider sqlDataTypeProvider)
		{
			var processor = new SqliteDataDefinitionExpressionAmmender(sqlDataTypeProvider);

			return processor.Visit(expression);
		}
		
		protected override Expression VisitSimpleConstraint(SqlSimpleConstraintExpression simpleConstraintExpression)
		{
			if (!this.currentIsPrimaryKey && simpleConstraintExpression.Constraint == SqlSimpleConstraint.AutoIncrement)
			{
				return null;
			}

			return base.VisitSimpleConstraint(simpleConstraintExpression);
		}

		protected override Expression VisitColumnDefinition(SqlColumnDefinitionExpression columnDefinitionExpression)
		{
			this.currentIsPrimaryKey = columnDefinitionExpression.ConstraintExpressions
				.OfType<SqlSimpleConstraintExpression>()
				.Any(c => c.Constraint == SqlSimpleConstraint.PrimaryKey);

			var retval = (SqlColumnDefinitionExpression)base.VisitColumnDefinition(columnDefinitionExpression);
			
			return retval;
		}
	}
}
