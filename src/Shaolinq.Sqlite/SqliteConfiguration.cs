// Copyright (c) 2007-2014 Thong Nguyen (tumtumtum@gmail.com)

using Shaolinq.Persistence;

namespace Shaolinq.Sqlite
{
	public static class SqliteConfiguration
	{
		public static DataAccessModelConfiguration Create(string fileName, string categories = null, bool useMonoData = false)
		{
			return new DataAccessModelConfiguration
			{
		       	SqlDatabaseContextInfos = new SqlDatabaseContextInfo[]
       			{
       				new SqliteSqlDatabaseContextInfo()
       				{
       					Categories = categories,
       					FileName = fileName,
						UseMonoData = useMonoData
       				},
       			}
			};
		}
	}
}
