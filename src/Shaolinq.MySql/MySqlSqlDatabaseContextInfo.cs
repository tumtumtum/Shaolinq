// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)

using Platform.Xml.Serialization;
using Shaolinq.Persistence;

namespace Shaolinq.MySql
{
	[XmlElement]
	public class MySqlSqlDatabaseContextInfo
		: SqlDatabaseContextInfo
	{
		[XmlAttribute]
		public string DatabaseName { get; set; }

		[XmlAttribute]
		public string ServerName { get; set; }

		[XmlAttribute]
		public string UserName { get; set; }

		[XmlAttribute]
		public string Password { get; set; }

		[XmlAttribute]
		public bool PoolConnections { get; set; }

		[XmlAttribute]
		public bool ConvertZeroDateTime { get; set; }

		[XmlAttribute]
		public bool AllowConvertZeroDateTime { get; set; }

		[XmlAttribute]
		public string SqlMode { get; set; }

		[XmlAttribute]
		public bool SilentlyIgnoreIndexConditions { get; set; }

		public MySqlSqlDatabaseContextInfo()
		{
			this.PoolConnections = true;
		}

		public override SqlDatabaseContext CreateSqlDatabaseContext(DataAccessModel model)
		{
			return MySqlSqlDatabaseContext.Create(this, model);
		}
	}
}

