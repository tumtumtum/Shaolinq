// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

using System.Data.Common;
using Shaolinq.Postgres.Shared;

namespace Shaolinq.Postgres
{
	public class PostgresDatabaseCreator
		: PostgresSharedDatabaseCreator
	{
		public PostgresDatabaseCreator(PostgresSqlDatabaseContext sqlDatabaseContext, DataAccessModel model)
			: base(model, sqlDatabaseContext, sqlDatabaseContext.DatabaseName)
		{
		}

		protected override string GetDatabaselessConnectionString()
		{
			return ((PostgresSqlDatabaseContext)this.sqlDatabaseContext).databaselessConnectionString;
		}

		protected override DbProviderFactory CreateDbProviderFactory()
		{
			return ((PostgresSqlDatabaseContext)this.sqlDatabaseContext).CreateDbProviderFactory();
		}
	}
}
