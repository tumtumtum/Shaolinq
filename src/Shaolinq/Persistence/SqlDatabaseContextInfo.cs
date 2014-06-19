// Copyright (c) 2007-2014 Thong Nguyen (tumtumtum@gmail.com)

using Platform.Xml.Serialization;

namespace Shaolinq.Persistence
{
	[XmlElement]
	public abstract class SqlDatabaseContextInfo
	{
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

		protected SqlDatabaseContextInfo()
		{
			this.CommandTimeout = 120;
			this.ConnectionTimeout = 60;
			this.SchemaName = ""; 
			this.TableNamePrefix = "";
			this.Categories = "";
		}

		public abstract SqlDatabaseContext CreateSqlDatabaseContext(DataAccessModel model);
	}
}
