using Platform.Xml.Serialization;

namespace Shaolinq
{
	[XmlElement]
	public abstract class PersistenceContextInfo
	{
		[XmlAttribute]
		public string ContextName { get; set; }
		public abstract string PersistenceContextName { get; set; }

		public abstract PersistenceContextProvider NewDatabaseContextProvider();
	}
}
