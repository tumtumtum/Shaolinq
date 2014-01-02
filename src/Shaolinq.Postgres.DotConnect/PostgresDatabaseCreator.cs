// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

using System.Data.Common;
using Shaolinq.Postgres.Shared;

namespace Shaolinq.Postgres.DotConnect
{
	public class PostgresDotConnectDatabaseCreator
		: PostgresSharedDatabaseCreator
	{
		public PostgresDotConnectDatabaseCreator(PostgresDotConnectSqlDatabaseContext sqlDatabaseContext, DataAccessModel model)
			: base(model, sqlDatabaseContext, sqlDatabaseContext.DatabaseName)
		{
		}

		protected override string GetDatabaselessConnectionString()
		{
			return ((PostgresDotConnectSqlDatabaseContext)this.sqlDatabaseContext).databaselessConnectionString;
		}

		protected override DbProviderFactory CreateDbProviderFactory()
		{
			return ((PostgresDotConnectSqlDatabaseContext)this.sqlDatabaseContext).CreateDbProviderFactory();
		}
	}
}
