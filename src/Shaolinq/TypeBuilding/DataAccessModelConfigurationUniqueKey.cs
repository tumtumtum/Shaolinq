// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Platform.Text;
using Platform.Xml.Serialization;

namespace Shaolinq.TypeBuilding
{
	[XmlElement]
	public class DataAccessModelConfigurationUniqueKey
	{
		[XmlElement("ConstraintDefaults")]
		public ConstraintDefaultsConfiguration ConstraintDefaultsConfiguration { get; set; }

		[XmlElement("NamingTransforms")]
		public NamingTransformsConfiguration NamingTransforms { get; set; }

		[XmlElement("ReferencedTypes")]
		[XmlListElement("Type", ItemType = typeof(Type), SerializeAsValueNode = true, ValueNodeAttributeName = "Name")]
		public List<Type> ReferencedTypes { get; set; }

		[XmlAttribute]
		public bool ValueTypesAutoImplicitDefault { get; set; }

		[XmlAttribute]
		public bool AlwaysSubmitDefaultValues { get; set; }

		[XmlAttribute]
		public bool IncludeImplicitDefaultsInSchema { get; set; }

		public byte[] GetSha1Bytes()
		{
			return SHA1.Create().ComputeHash(Encoding.UTF8.GetBytes(XmlSerializer<DataAccessModelConfigurationUniqueKey>.New().SerializeToString(this)));
		}

		public string GetSha1Hex()
		{
			return TextConversion.ToHexString(GetSha1Bytes());
		}

		public DataAccessModelConfigurationUniqueKey(DataAccessModelConfiguration config)
		{
			this.ConstraintDefaultsConfiguration = config.ConstraintDefaultsConfiguration;
			this.NamingTransforms = config.NamingTransforms;
			this.ReferencedTypes = config.ReferencedTypes;
			this.ValueTypesAutoImplicitDefault = config.ValueTypesAutoImplicitDefault;
			this.AlwaysSubmitDefaultValues = config.AlwaysSubmitDefaultValues;
			this.IncludeImplicitDefaultsInSchema = config.IncludeImplicitDefaultsInSchema;
		}
	}
}