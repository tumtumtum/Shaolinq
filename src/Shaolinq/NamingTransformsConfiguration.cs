// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using Platform.Xml.Serialization;

namespace Shaolinq
{
	[XmlElement]
	public class NamingTransformsConfiguration
	{
		public const string DefaultForeignKeyConstraintName = "s/^.*$/fkc_$(TABLENAME)_$(COLUMNNAME)/";

		[XmlAttribute]
		public string DataAccessObjectName { get; set; }

		[XmlAttribute]
		public string PersistedMemberName { get; set; }

		[XmlAttribute]
		public string PersistedMemberPrefixName { get; set; }

		[XmlAttribute]
		public string PersistedMemberSuffixName { get; set; }

		[XmlAttribute]
		public string ForeignKeyConstraintName { get; set; }
	}
}