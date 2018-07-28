// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)

using System;
using Platform;
using Platform.Xml.Serialization;

namespace Shaolinq
{
	internal class SqlDatabaseContextInfoDynamicTypeProvider
		: IXmlListElementDynamicTypeProvider
	{
		public Type GetType(object instance)
		{
			return instance.GetType();
		}

		public string GetName(object instance)
		{
			return instance.GetType().Name.ReplaceLast("Info", "");
		}

		public SqlDatabaseContextInfoDynamicTypeProvider(SerializationMemberInfo memberInfo, TypeSerializerCache cache, SerializerOptions options)
		{
		}

		public Type GetType(System.Xml.XmlReader reader)
		{
			Type type;
			string typeName;

			if (string.IsNullOrEmpty(typeName = reader.GetAttribute("Type")))
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

				throw new NotSupportedException($"ContextProviderType: {reader.Name}, tried: {typeName}");
			}
			else
			{
				type = Type.GetType(typeName, false);

				if (type != null)
				{
					return type;
				}

				throw new NotSupportedException($"ContextProviderType: {reader.Name}.  Tried Explicit: {typeName}");
			}
		}
	}
}