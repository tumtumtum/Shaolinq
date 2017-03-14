// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using Platform.Text;
using Platform.Xml.Serialization;
using Shaolinq.Persistence;

namespace Shaolinq
{
	[XmlElement]
	public class DataAccessModelConfiguration
	{
		[XmlElement("SqlDatabaseContexts")]
		[XmlListElementDynamicTypeProvider(typeof(SqlDatabaseContextInfoDynamicTypeProvider))]
		public SqlDatabaseContextInfo[] SqlDatabaseContextInfos { get; set; }

		[XmlElement("ConstraintDefaultsConfiguration")]
		public ConstraintDefaultsConfiguration ConstraintDefaultsConfiguration { get; set; }

		[XmlElement("NamingTransforms")]
		public NamingTransformsConfiguration NamingTransforms { get; set; }

		[XmlElement("ReferencedTypes")]
		[XmlListElement("Type", ItemType = typeof(Type), SerializeAsValueNode = true, ValueNodeAttributeName = "Name")]
		public Type[] ReferencedTypes { get; set; }

		[XmlAttribute]
		public bool? SaveAndReuseGeneratedAssemblies { get; set; } = true;

		[XmlAttribute]
		public string GeneratedAssembliesSaveDirectory { get; set; }

		public DataAccessModelConfiguration()
		{
			this.SqlDatabaseContextInfos = new SqlDatabaseContextInfo[0];
			this.ConstraintDefaultsConfiguration = new ConstraintDefaultsConfiguration();
		}

		public byte[] GetSha1Bytes()
		{
			return Encoding.UTF8.GetBytes(XmlSerializer<DataAccessModelConfiguration>.New().SerializeToString(this));
		}

		public string GetSha1Hex()
		{
			return TextConversion.ToHexString(this.GetSha1Bytes());
		}
	}
}
