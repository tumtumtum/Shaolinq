// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using Platform.Xml.Serialization;

namespace Shaolinq
{
	[XmlElement]
	public class NamingTransformsConfiguration
	{
		public const string DefaultForeignKeyConstraintName = "s/^.*$/fk_$(TABLENAME:L)_$(PROPERTYNAME:L)/";
		public const string DefaultPrimaryKeyConstraintName = "s/^.*$/pk_$(TABLENAME:L)_$(PROPERTYNAME:L)/";
		public const string DefaultDefaultValueConstraintName = "s/^.*$/$(TABLENAME:L)_$(PROPERTYNAME:L)_def/";

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
		public string PrimaryKeyConstraintName { get; set; }

		[XmlAttribute]
		public string DefaultValueConstraintName { get; set; }
	}
}