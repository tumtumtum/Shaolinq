// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using Platform.Xml.Serialization;

namespace Shaolinq
{
	[XmlElement]
	public class NamingTransformsConfiguration
	{
		[XmlAttribute]
		public string DataAccessObjectName { get; set; }

		[XmlAttribute]
		public string PersistedMemberName { get; set; }

		[XmlAttribute]
		public string PersistedMemberPrefixName { get; set; }

		[XmlAttribute]
		public string PersistedMemberSuffixName { get; set; }
	}
}