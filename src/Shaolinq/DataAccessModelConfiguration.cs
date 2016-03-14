// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System.Security.Cryptography;
using System.Text;
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

		public DataAccessModelConfiguration()
		{
			this.SqlDatabaseContextInfos = new SqlDatabaseContextInfo[0];
			this.ConstraintDefaultsConfiguration = new ConstraintDefaultsConfiguration();
		}

		public string GetMd5()
		{
			return TextConversion.ToHexString(new MD5CryptoServiceProvider().ComputeHash(Encoding.UTF8.GetBytes(XmlSerializer<DataAccessModelConfiguration>.New().SerializeToString(this))));
		}

		public string GetSha256()
		{
			return TextConversion.ToHexString(new SHA256CryptoServiceProvider().ComputeHash(Encoding.UTF8.GetBytes(XmlSerializer<DataAccessModelConfiguration>.New().SerializeToString(this))));
		}
	}
}
