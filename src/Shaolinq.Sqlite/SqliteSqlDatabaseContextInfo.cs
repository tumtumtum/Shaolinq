// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Runtime.InteropServices;
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

		[XmlAttribute]
		public bool UseMonoData { get; set; }

		public override SqlDatabaseContext CreateSqlDatabaseContext(DataAccessModel model)
		{
			if (this.UseMonoData)
			{
				return SqliteMonoSqlDatabaseContext.Create(this, model);
			}
			else
			{
				return SqliteOfficialsSqlDatabaseContext.Create(this, model);
			}
		}
	}
}
