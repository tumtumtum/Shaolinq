using System.Linq.Expressions;
using Shaolinq.Persistence;
using Shaolinq.Persistence.Linq;
using Shaolinq.Persistence.Linq.Expressions;

namespace Shaolinq.Postgres.DotConnect
{
	public class PostgresDotConnectSqlQueryFormatter
		: PostgresSqlQueryFormatter
	{
		public PostgresDotConnectSqlQueryFormatter(SqlQueryFormatterOptions options, SqlDialect sqlDialect, SqlDataTypeProvider sqlDataTypeProvider, string schemaName)
			: base(options, sqlDialect, sqlDataTypeProvider, schemaName)
		{
		}

		protected override Expression VisitColumn(SqlColumnExpression columnExpression)
		{
			return base.OriginalVisitColumn(columnExpression);
		}
	}
}
