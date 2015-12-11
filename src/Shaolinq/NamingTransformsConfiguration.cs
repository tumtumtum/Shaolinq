// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using Platform.Xml.Serialization;

namespace Shaolinq
{
	[XmlElement]
	public class NamingTransformsConfiguration
	{
		public string DataAccessObjectName { get; set; }
		public string PersistedMemberName { get; set; }
		public string PersistedMemberPrefixName { get; set; }
		public string PersistedMemberSuffixName { get; set; }
	}
}