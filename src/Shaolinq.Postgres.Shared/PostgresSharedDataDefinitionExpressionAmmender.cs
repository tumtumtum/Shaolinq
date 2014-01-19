// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

using System.Linq.Expressions;
using Shaolinq.Persistence;
using Shaolinq.Persistence.Linq.Expressions;

namespace Shaolinq.Postgres.Shared
{
	public class PostgresSharedDataDefinitionExpressionAmmender
		: SqlExpressionVisitor
	{
		private readonly SqlDataTypeProvider sqlDataTypeProvider;
		private bool foundPrimaryKeyAutoIncrementColumnConstraint;

		private PostgresSharedDataDefinitionExpressionAmmender(SqlDataTypeProvider sqlDataTypeProvider)
		{
			this.sqlDataTypeProvider = sqlDataTypeProvider;
		}

		public static Expression Ammend(Expression expression, SqlDataTypeProvider sqlDataTypeProvider)
		{
			var processor = new PostgresSharedDataDefinitionExpressionAmmender(sqlDataTypeProvider);

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
				var longTypeSqlName = sqlDataTypeProvider.GetSqlDataType(typeof(long)).GetSqlName(null);

				if (((SqlTypeExpression)columnDefinitionExpression.ColumnTypeName).TypeName == longTypeSqlName)
				{
					retval = new SqlColumnDefinitionExpression(retval.ColumnName, new SqlTypeExpression("BIGSERIAL"), retval.ConstraintExpressions);
				}
				else
				{
					retval = new SqlColumnDefinitionExpression(retval.ColumnName, new SqlTypeExpression("SERIAL"), retval.ConstraintExpressions);
				}
			}

			return retval;
		}
	}
}
