// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

 using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Platform;
using Platform.Xml.Serialization;
 using Shaolinq.Persistence.Sql;

namespace Shaolinq
{
	[XmlElement]
	public class DataAccessModelConfiguration
	{
		public class DatabaseConnectionInfoDynamicTypeProvider
			: IXmlListElementDynamicTypeProvider
		{
			private static readonly Regex NameRegex = new Regex("([a-zA-Z0-9]+)DatabaseConnection");

			public DatabaseConnectionInfoDynamicTypeProvider(SerializationMemberInfo memberInfo, TypeSerializerCache cache, SerializerOptions options)
			{
			}

			public Type GetType(System.Xml.XmlReader reader)
			{
				Type type;
				string typeName;

				if (String.IsNullOrEmpty(typeName = reader.GetAttribute("Type")))
				{
					var match = NameRegex.Match(reader.Name);
					var provider = match.Groups[1].Value;
					var namespaceName = "Shaolinq." + provider;

					type = Type.GetType(String.Concat(namespaceName, ".", reader.Name, "Info"), false);

					if (type != null)
					{
						return type;
					}

					var fullname = String.Concat(namespaceName, ".", reader.Name, "Info, ", namespaceName);
					
					type = Type.GetType(fullname, false);

					if (type != null)
					{
						return type;
					}

					throw new NotSupportedException(String.Format("ContextProviderType: {0}, tried: {1}", reader.Name, fullname));
				}
				else
				{
					type = Type.GetType(typeName, false);

					if (type != null)
					{
						return type;
					}

					throw new NotSupportedException(String.Format("ContextProviderType: {0}.  Tried Explicit: {1}" + reader.Name, type));
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
		
		[XmlElement("DatabaseConnections")]
		[XmlListElementDynamicTypeProvider(typeof(DatabaseConnectionInfoDynamicTypeProvider))]
		public DatabaseConnectionInfo[] DatabaseConnectionInfos
		{
			get;
			set;
		}
	}
}
