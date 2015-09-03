// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System;
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
		public class SqlDatabaseContextInfoDynamicTypeProvider
			: IXmlListElementDynamicTypeProvider
		{
			public SqlDatabaseContextInfoDynamicTypeProvider(SerializationMemberInfo memberInfo, TypeSerializerCache cache, SerializerOptions options)
			{
			}

			public Type GetType(System.Xml.XmlReader reader)
			{
				Type type;
				string typeName;

				if (String.IsNullOrEmpty(typeName = reader.GetAttribute("Type")))
				{
					var classPrefix = reader.Name.Replace("-", "");
					var namespaceName = "Shaolinq." + reader.Name.Replace("-", ".");

					typeName = namespaceName + "." + classPrefix + "SqlDatabaseContextInfo";

					type = Type.GetType(typeName, false);

					if (type != null)
					{
						return type;
					}

					typeName = typeName + ", " + namespaceName;

					type = Type.GetType(typeName, false);

					if (type != null)
					{
						return type;
					}

					throw new NotSupportedException(String.Format("ContextProviderType: {0}, tried: {1}", reader.Name, typeName));
				}
				else
				{
					type = Type.GetType(typeName, false);

					if (type != null)
					{
						return type;
					}

					throw new NotSupportedException(String.Format("ContextProviderType: {0}.  Tried Explicit: {1}" + reader.Name, typeName));
				}
			}

			public Type GetType(object instance)
			{
				return instance.GetType();
			}

			public string GetName(object instance)
			{
				return instance.GetType().Name.ReplaceLast("Info", "");
			}
		}
		
		[XmlElement("SqlDatabaseContexts")]
		[XmlListElementDynamicTypeProvider(typeof(SqlDatabaseContextInfoDynamicTypeProvider))]
		public SqlDatabaseContextInfo[] SqlDatabaseContextInfos
		{
			get;
			set;
		}

		[XmlElement("ConstraintDefaults")]
		public ConstraintDefaults ConstraintDefaults
		{
			get;
			set;
		}

		public DataAccessModelConfiguration()
		{
			this.SqlDatabaseContextInfos = new SqlDatabaseContextInfo[0];
			this.ConstraintDefaults = ConstraintDefaults.Default;
		}

		public string GetMd5()
		{
			var md5 = new MD5CryptoServiceProvider();

			return TextConversion.ToHexString(md5.ComputeHash(Encoding.UTF8.GetBytes(string.Empty)));
		}
	}
}
