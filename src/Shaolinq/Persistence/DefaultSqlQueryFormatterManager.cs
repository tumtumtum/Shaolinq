// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)

using System.Data;
using Shaolinq.Persistence.Linq;

namespace Shaolinq.Persistence
{
	public class DefaultSqlQueryFormatterManager
		: SqlQueryFormatterManager
	{
		public SqlDialect SqlDialect { get; }
		public SqlQueryFormatterConstructorMethod ConstructorMethod { get; }
		public delegate SqlQueryFormatter SqlQueryFormatterConstructorMethod(SqlQueryFormatterOptions options, IDbConnection connection);

		public DefaultSqlQueryFormatterManager(SqlDialect sqlDialect, NamingTransformsConfiguration namingTransformsConfiguration, SqlQueryFormatterConstructorMethod constructorMethod)
			: base(sqlDialect, namingTransformsConfiguration)
		{
			this.SqlDialect = sqlDialect;
			this.ConstructorMethod = constructorMethod;
		}

		public override SqlQueryFormatter CreateQueryFormatter(SqlQueryFormatterOptions options = SqlQueryFormatterOptions.Default, IDbConnection connection = null)
		{
			return this.ConstructorMethod(options, connection);
		}
	}
}
