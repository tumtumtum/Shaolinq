// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

using Shaolinq.Persistence;

namespace Shaolinq.Sqlite
{
	public static class SqliteConfiguration
	{
		public static DataAccessModelConfiguration Create(string fileName)
		{
			return Create(fileName, null);
		}

		public static DataAccessModelConfiguration Create(string fileName, string categories)
		{
			return new DataAccessModelConfiguration
			{
		       	SqlDatabaseContextInfos = new SqlDatabaseContextInfo[]
       			{
       				new SqliteDatabaseConnectionInfo()
       				{
       					Categories = categories,
       					FileName = fileName
       				},
       			}
			};
		}
	}
}
