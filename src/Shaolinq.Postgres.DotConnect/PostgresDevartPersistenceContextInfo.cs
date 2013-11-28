// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

 using Platform.Xml.Serialization;

namespace Shaolinq.Postgres.DotConnect
{
	[XmlElement]
	public class PostgresDotConnectPersistenceContextInfo
		: PersistenceContextInfo
	{
		[XmlAttribute]
		public string DatabaseName { get; set; }

		[XmlElement("Connections")]
		[XmlListElement(typeof(PostgresDotConnectDatabaseConnectionInfo), "Connection")]
		public PostgresDotConnectDatabaseConnectionInfo[] DatabaseConnectionInfos { get; set; }

		public override string PersistenceContextName
		{
			get { return this.DatabaseName; }
			set { this.DatabaseName = value; }
		}

		public override PersistenceContextProvider NewDatabaseContextProvider()
		{
			return new PostgresDotConnectPersistenceContextProvider(this.ContextName, this.DatabaseName, this.DatabaseConnectionInfos);
		}
	}
}
