// Copyright (c) 2007-2014 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using Platform.Xml.Serialization;
using Shaolinq.Persistence;

namespace Shaolinq.Sqlite
{
	[XmlElement]
	public class SqliteSqlDatabaseContextInfo
		: SqlDatabaseContextInfo
	{
		[XmlAttribute]
		public string FileName { get; set; }

		public override SqlDatabaseContext CreateSqlDatabaseContext(DataAccessModel model)
		{
			var useMonoData = Environment.GetEnvironmentVariable("SHAOLINQ_USE_MONO_DATA_SQLITE");

			if (!string.IsNullOrEmpty(useMonoData) && SqliteSqlDatabaseContext.IsRunningMono())
			{
				return SqliteMonoSqlDatabaseContext.Create(this, model);
			}
			else
			{
				SqliteRuntimeOfficialAssemblyManager.Init();

				var retval = SqliteOfficialsSqlDatabaseContext.Create(this, model);

				return retval;
			}
		}
	}
}
