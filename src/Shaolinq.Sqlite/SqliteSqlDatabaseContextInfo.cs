// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

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
				return SqliteOfficialSqlDatabaseContext.Create(this, model);
			}
		}
	}
}
