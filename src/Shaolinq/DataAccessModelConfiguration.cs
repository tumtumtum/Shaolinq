// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Text.RegularExpressions;
using Platform;
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
					var provider = reader.Name;
					var namespaceName = "Shaolinq." + provider;

					type = Type.GetType(String.Concat(namespaceName, ".", provider, "SqlDatabaseContextInfo"), false);

					if (type != null)
					{
						return type;
					}

					var fullname = String.Concat(namespaceName, ".", provider, "SqlDatabaseContextInfo", ", ", namespaceName);

					type = Type.GetType(fullname, false);

					if (type != null)
					{
						return type;
					}

					throw new NotSupportedException(String.Format("ContextProviderType: {0}, tried: {1}", fullname, provider));
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
	}
}
