// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

using System.Data.Common;
using Shaolinq.Postgres.Shared;

namespace Shaolinq.Postgres.DotConnect
{
	public class PostgresDotConnectDatabaseCreator
		: PostgresSharedDatabaseCreator
	{
		public PostgresDotConnectDatabaseCreator(PostgresDotConnectDatabaseConnection connection, DataAccessModel model)
			: base(connection, model)
		{
		}

		protected override string GetDatabaselessConnectionString()
		{
			return ((PostgresDotConnectDatabaseConnection)this.connection).databaselessConnectionString;
		}

		protected override DbProviderFactory CreateDbProviderFactory()
		{
			return ((PostgresDotConnectDatabaseConnection)this.connection).NewDbProviderFactory();
		}
	}
}
