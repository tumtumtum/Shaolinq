// Copyright (c) 2007-2014 Thong Nguyen (tumtumtum@gmail.com)

using Platform.Xml.Serialization;

namespace Shaolinq.Persistence
{
	[XmlElement]
	public abstract class SqlDatabaseContextInfo
	{
		[XmlAttribute]
		public string ConnectionString { get; set; }

		[XmlAttribute]
		public string Categories { get; set; }

		[XmlAttribute]
		public int CommandTimeout { get; set; }

		[XmlAttribute]
		public int ConnectionTimeout { get; set; }

		[XmlAttribute]
		public string TableNamePrefix { get; set; }

		[XmlAttribute]
		public string SchemaName { get; set; }

		public const int DefaultCommandTimeout = 120;
		public const int DefaultConnectionTimeout = 60;

		protected SqlDatabaseContextInfo()
		{
			this.CommandTimeout = DefaultCommandTimeout;
			this.ConnectionTimeout = DefaultConnectionTimeout;
			this.SchemaName = ""; 
			this.TableNamePrefix = "";
			this.Categories = "";
		}

		public abstract SqlDatabaseContext CreateSqlDatabaseContext(DataAccessModel model);
	}
}
