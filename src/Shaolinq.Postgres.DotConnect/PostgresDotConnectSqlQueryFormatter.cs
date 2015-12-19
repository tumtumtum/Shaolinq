// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Linq.Expressions;
using Shaolinq.Persistence;
using Shaolinq.Persistence.Linq;
using Shaolinq.Persistence.Linq.Expressions;

namespace Shaolinq.Postgres.DotConnect
{
	public class PostgresDotConnectSqlQueryFormatter
		: PostgresSqlQueryFormatter
	{
		private bool visitingColumn = false;

		public PostgresDotConnectSqlQueryFormatter(SqlQueryFormatterOptions options, SqlDialect sqlDialect, SqlDataTypeProvider sqlDataTypeProvider, string schemaName, bool convertEnumsToText)
			: base(options, sqlDialect, sqlDataTypeProvider, schemaName, convertEnumsToText)
		{
		}

		protected override void VisitColumn(SqlSelectExpression selectExpression, SqlColumnDeclaration column)
		{
			this.visitingColumn = true;

			base.VisitColumn(selectExpression, column);

			this.visitingColumn = false;
		}

		protected override Expression VisitConstant(ConstantExpression constantExpression)
		{
			if (!visitingColumn)
			{
				return base.VisitConstant(constantExpression);
			}

			var retval = base.VisitConstant(constantExpression);

			var dataType = this.sqlDataTypeProvider.GetSqlDataType(constantExpression.Type);

			if (dataType != null)
			{
				this.Write("::");
				this.Write(dataType.GetSqlName(null));
			}

			return retval;
		}
	}
}
