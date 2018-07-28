// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)

using System.Linq.Expressions;
using Platform;
using Shaolinq.Persistence;
using Shaolinq.Persistence.Linq;

namespace Shaolinq.Postgres.DotConnect
{
	public class PostgresDotConnectSqlQueryFormatter
		: PostgresSqlQueryFormatter
	{
		public PostgresDotConnectSqlQueryFormatter(SqlQueryFormatterOptions options, SqlDialect sqlDialect, SqlDataTypeProvider sqlDataTypeProvider, TypeDescriptorProvider typeDescriptorProvider, string schemaName, bool convertEnumsToText)
			: base(options, sqlDialect, sqlDataTypeProvider, typeDescriptorProvider, schemaName, convertEnumsToText)
		{
		}

		protected override Expression VisitConstant(ConstantExpression constantExpression)
		{
			if (this.ConvertEnumsToText && constantExpression.Type.GetUnwrappedNullableType().IsEnum)
			{
				return constantExpression;
			}

			var retval = base.VisitConstant(constantExpression);

			var dataType = this.sqlDataTypeProvider.GetSqlDataType(constantExpression.Type);

			if (dataType != null)
			{
				Write("::");
				Write(dataType.GetSqlName(null));
			}

			return retval;
		}
	}
}
