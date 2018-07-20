// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using Platform.Xml.Serialization;

namespace Shaolinq
{
	[XmlElement]
	public class NamingTransformsConfiguration
	{
		public const string DefaultForeignKeyConstraintName = "s/^\\s*$/fk_$(TABLENAME:L)_$(PROPERTYNAMES:L)/";
		public const string DefaultIndexConstraintName = "s/^\\s*$/idx_$(TABLENAME:L)_$(PROPERTYNAMES:L)/";
		public const string DefaultPrimaryKeyConstraintName = "s/^\\s*$/pk_$(TABLENAME:L)_$(PROPERTYNAMES:L)/";
		public const string DefaultDefaultValueConstraintName = "s/^\\s*$/$(TABLENAME:L)_$(COLUMNNAME:L)_def/";

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