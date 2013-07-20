using Platform.Xml.Serialization;

namespace Shaolinq.Persistence.Sql.Sqlite
{
	[XmlElement]
	public class SqliteDatabaseConnectionInfo
		: DatabaseConnectionInfo
	{
		[XmlAttribute]
		public string FileName { get; set; }
	}
}