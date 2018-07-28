// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)

using System.Linq;
using System.Linq.Expressions;
using Shaolinq.Persistence;
using Shaolinq.Persistence.Linq;
using Shaolinq.Persistence.Linq.Expressions;

namespace Shaolinq.Postgres
{
	public class PostgresDataDefinitionExpressionAmender
		: SqlExpressionVisitor
	{
		private readonly SqlDataTypeProvider sqlDataTypeProvider;
		private bool currentIsPrimaryKey;

		private PostgresDataDefinitionExpressionAmender(SqlDataTypeProvider sqlDataTypeProvider)
		{
			this.sqlDataTypeProvider = sqlDataTypeProvider;
		}

		public static Expression Amend(Expression expression, SqlDataTypeProvider sqlDataTypeProvider)
		{
			var processor = new PostgresDataDefinitionExpressionAmender(sqlDataTypeProvider);

			return processor.Visit(expression);
		}
		
		protected override Expression VisitConstraint(SqlConstraintExpression expression)
		{
			if (this.currentIsPrimaryKey && (expression.ConstraintType & ConstraintType.AutoIncrement) != 0)
			{
				return null;
			}

			return base.VisitConstraint(expression);
		}

		protected override Expression VisitColumnDefinition(SqlColumnDefinitionExpression columnDefinitionExpression)
		{
			this.currentIsPrimaryKey = columnDefinitionExpression.ConstraintExpressions
				.Any(c => (c.ConstraintType & ConstraintType.PrimaryKey) != 0);

			var isAutoIncrement = columnDefinitionExpression.ConstraintExpressions
				.Any(c => (c.ConstraintType & ConstraintType.AutoIncrement) != 0);

			var retval = (SqlColumnDefinitionExpression)base.VisitColumnDefinition(columnDefinitionExpression);

			if (isAutoIncrement)
			{
				var longTypeSqlName = this.sqlDataTypeProvider.GetSqlDataType(typeof(long)).GetSqlName(null);

				if (((SqlTypeExpression)columnDefinitionExpression.ColumnType).TypeName == longTypeSqlName)
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
