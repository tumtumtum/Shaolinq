// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using Platform.Xml.Serialization;
using Shaolinq.Persistence;

namespace Shaolinq.SqlServer
{
	[XmlElement]
	public class SqlServerSqlDatabaseContextInfo
		: SqlDatabaseContextInfo
	{
		[XmlAttribute]
		public string DatabaseName { get; set; }

		[XmlAttribute]
		public string ServerName { get; set; }

		[XmlAttribute]
		public string Instance { get; set; }

		[XmlAttribute]
		public string UserName { get; set; }

		[XmlAttribute]
		public string Password { get; set; }

		[XmlAttribute]
		public bool Encrypt { get; set; }

		[XmlAttribute]
		public bool TrustedConnection { get; set; } = false;

		[XmlAttribute]
		public bool DeleteDatabaseDropsTablesOnly { get; set; } = false;

		[XmlAttribute]
		public bool MultipleActiveResultSets { get; set; } = true;

		public override SqlDatabaseContext CreateSqlDatabaseContext(DataAccessModel model)
		{
			return SqlServerSqlDatabaseContext.Create(this, model);
		}
	}
}
