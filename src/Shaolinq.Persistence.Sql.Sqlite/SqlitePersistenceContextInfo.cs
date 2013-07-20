using Platform.Xml.Serialization;

namespace Shaolinq.Persistence.Sql.Sqlite
{
	[XmlElement]
	public class SqlitePersistenceContextInfo
		: PersistenceContextInfo
	{
		[XmlAttribute]
		public string DatabaseName { get; set; }

		[XmlElement("Connections")]
		[XmlListElement(typeof(SqliteDatabaseConnectionInfo), "Connection")]
		public SqliteDatabaseConnectionInfo[] DatabaseConnectionInfos { get; set; }

		public override string PersistenceContextName
		{
			get { return this.DatabaseName; }
			set { this.DatabaseName = value; }
		}

		public override PersistenceContextProvider NewDatabaseContextProvider()
		{
			return new SqlitePersistenceContextProvider(this.ContextName, this.DatabaseName, this.DatabaseConnectionInfos);
		}
	}
}