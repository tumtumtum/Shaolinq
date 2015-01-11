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

		public override SqlDatabaseContext CreateSqlDatabaseContext(DataAccessModel model)
		{
			return SqlServerSqlDatabaseContext.Create(this, model);
		}
	}
}
