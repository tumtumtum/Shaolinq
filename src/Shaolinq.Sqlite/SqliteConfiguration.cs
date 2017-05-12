// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Shaolinq.Persistence;

namespace Shaolinq.Sqlite
{
	public static class SqliteConfiguration
	{
		private static readonly Regex connectionStringRegex = new Regex(@"\s*Data Source\s*=", RegexOptions.Compiled);
		
		public static DataAccessModelConfiguration Create(string connectionStringOrFileName, string categories = null, bool useMonoData = false)
		{
			string fileName = null;
			string connectionString = null;

			if (connectionStringRegex.IsMatch(connectionStringOrFileName) || connectionStringOrFileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
			{
				connectionString = connectionStringOrFileName;
			}
			else
			{
				fileName = connectionStringOrFileName;
			}
			
			return new DataAccessModelConfiguration
			{
				SqlDatabaseContextInfos = new List<SqlDatabaseContextInfo>
				{
					new SqliteSqlDatabaseContextInfo
					{
						Categories = categories,
						FileName = fileName,
						UseMonoData = useMonoData,
						ConnectionString = connectionString
					},
				}
			};
		}
	}
}
