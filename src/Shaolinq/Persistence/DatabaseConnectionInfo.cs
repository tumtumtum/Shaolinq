// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

using Platform.Xml.Serialization;

namespace Shaolinq.Persistence
{
	[XmlElement]
	public abstract class DatabaseConnectionInfo
	{
		[XmlAttribute]
		public DatabaseReadMode DatabaseReadMode { get; set; }

		[XmlAttribute]
		public int CommandTimeout { get; set; }

		[XmlAttribute]
		public int ConnectionTimeout { get; set; }

		[XmlAttribute]
		public string SchemaNamePrefix { get; set; }

		protected DatabaseConnectionInfo()
		{
			this.CommandTimeout = 120;
			this.ConnectionTimeout = 60;
			this.SchemaNamePrefix = "";
			this.DatabaseReadMode = DatabaseReadMode.ReadWrite;
		}

		public abstract SqlDatabaseContext CreateSqlDatabaseContext();
	}
}
