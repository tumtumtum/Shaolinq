// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

using Shaolinq.Persistence.Sql;

namespace Shaolinq.Sqlite
{
	public static class SqliteConfiguration
	{
		public static DataAccessModelConfiguration Create(string fileName)
		{
			return Create(fileName, DatabaseReadMode.ReadWrite);
		}

		public static DataAccessModelConfiguration Create(string fileName, DatabaseReadMode databaseReadMode)
		{
			return new DataAccessModelConfiguration
			{
		       	DatabaseConnectionInfos = new DatabaseConnectionInfo[]
       			{
       				new SqliteDatabaseConnectionInfo()
       				{
       					DatabaseReadMode = databaseReadMode,
       					FileName = fileName
       				},
       			}
			};
		}
	}
}
