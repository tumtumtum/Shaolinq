// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Platform;
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

		public string GetSha256()
		{
			var sha256 = new SHA256CryptoServiceProvider();

			return TextConversion.ToHexString(sha256.ComputeHash(Encoding.UTF8.GetBytes(XmlSerializer<DataAccessModelConfiguration>.New().SerializeToString(this))));
		}
	}
}
