// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

using System.Data.Common;
using Shaolinq.Postgres.Shared;

namespace Shaolinq.Postgres
{
	public class PostgresDatabaseCreator
		: PostgresSharedDatabaseCreator
	{
		public PostgresDatabaseCreator(PostgresSqlDatabaseContext connection, DataAccessModel model)
			: base(connection, model)
		{
		}

		protected override string GetDatabaselessConnectionString()
		{
			return ((PostgresSqlDatabaseContext)this.connection).databaselessConnectionString;
		}

		protected override DbProviderFactory CreateDbProviderFactory()
		{
			return ((PostgresSqlDatabaseContext)this.connection).NewDbProviderFactory();
		}
	}
}
