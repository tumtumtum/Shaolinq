using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Platform;
using Shaolinq.Persistence;
using Shaolinq.Persistence.Linq;
using Shaolinq.Persistence.Linq.Expressions;
using Shaolinq.Postgres.Shared;

namespace Shaolinq.Postgres
{
	public class PostgresQueryFormatter
		: PostgresSharedSqlQueryFormatter
	{
		private int selectNesting = 0;

		public PostgresQueryFormatter(SqlQueryFormatterOptions options, SqlDialect sqlDialect, SqlDataTypeProvider sqlDataTypeProvider, string schemaName)
			: base(options, sqlDialect, sqlDataTypeProvider, schemaName)
		{
		}

		protected override Expression VisitSelect(SqlSelectExpression selectExpression)
		{
			selectNesting++;

			var retval = base.VisitSelect(selectExpression);

			selectNesting--;

			return retval;
		}

		protected override Expression VisitColumn(SqlColumnExpression columnExpression)
		{
			if (selectNesting == 1 && columnExpression.Type.GetUnwrappedNullableType().IsEnum)
			{
				base.VisitColumn(columnExpression);
				this.Write("::TEXT");

				return columnExpression;
			}

			return base.VisitColumn(columnExpression);
		}
	}
}
