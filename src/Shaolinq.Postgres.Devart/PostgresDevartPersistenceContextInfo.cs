// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

 using Platform.Xml.Serialization;

namespace Shaolinq.Postgres.Devart
{
	[XmlElement]
	public class PostgresDevartPersistenceContextInfo
		: PersistenceContextInfo
	{
		[XmlAttribute]
		public string DatabaseName { get; set; }

		[XmlElement("Connections")]
		[XmlListElement(typeof(PostgresDevartDatabaseConnectionInfo), "Connection")]
		public PostgresDevartDatabaseConnectionInfo[] DatabaseConnectionInfos { get; set; }

		public override string PersistenceContextName
		{
			get { return this.DatabaseName; }
			set { this.DatabaseName = value; }
		}

		public override PersistenceContextProvider NewDatabaseContextProvider()
		{
			return new PostgresDevartPersistenceContextProvider(this.ContextName, this.DatabaseName, this.DatabaseConnectionInfos);
		}
	}
}
