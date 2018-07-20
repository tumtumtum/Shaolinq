// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using Platform.Xml.Serialization;

namespace Shaolinq
{
	[XmlElement]
	public class NamingTransformsConfiguration
	{
		public const string DefaultForeignKeyConstraintName = "s/^\\s*$/fk_$(PERSISTED_TYPENAME:L)_$(PERSISTED_PROPERTYSUFFIXNAMES:L)/";
		public const string DefaultIndexConstraintName = "s/^\\s*$/idx_$(PERSISTED_TYPENAME:L)_$(PERSISTED_PROPERTYSUFFIXNAMES:L)/";
		public const string DefaultPrimaryKeyConstraintName = "s/^\\s*$/pk_$(PERSISTED_TYPENAME:L)_$(PERSISTED_PROPERTYSUFFIXNAMES:L)/";
		public const string DefaultDefaultValueConstraintName = "s/^\\s*$/def_$(PERSISTED_TYPENAME:L)_$(PERSISTED_PROPERTYSUFFIXNAMES:L)/";

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

		[XmlAttribute]
		public string IndexConstraintName { get; set; }

		[XmlAttribute]
		public string PrimaryKeyConstraintName { get; set; }

		[XmlAttribute]
		public string DefaultValueConstraintName { get; set; }
	}
}