// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

using System;
using Platform.Xml.Serialization;
﻿using Shaolinq.Persistence;
﻿
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
				return SqliteWindowsSqlDatabaseContext.Create(this, model);
			}
		}
	}
}
