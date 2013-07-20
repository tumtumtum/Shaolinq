using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Platform;
using Platform.Xml.Serialization;

namespace Shaolinq
{
	[XmlElement]
	public class DataAccessModelConfiguration
	{
		public class PersistenceContextInfoDynamicTypeProvider
			: IXmlListElementDynamicTypeProvider
		{
			private static readonly Regex NameRegex = new Regex("([a-zA-Z0-9]+)PersistenceContext");

			public PersistenceContextInfoDynamicTypeProvider(SerializationMemberInfo memberInfo, TypeSerializerCache cache, SerializerOptions options)
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
					var namespaceName = "Shaolinq.Persistence.Sql." + provider;

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

		private readonly IDictionary<string, PersistenceContextProvider> databaseContextProviders = new Dictionary<string, PersistenceContextProvider>();

		[XmlElement]
		[XmlListElementDynamicTypeProvider(typeof(PersistenceContextInfoDynamicTypeProvider))]
		public PersistenceContextInfo[] PersistenceContexts
		{
			get;
			set;
		}

		public bool TryGetDatabaseContextProvider(string contextName, out PersistenceContextProvider persistenceContextProvider)
		{
			if (this.databaseContextProviders.TryGetValue(contextName, out persistenceContextProvider))
			{
				return true;
			}

			foreach (var persistenceContextInfo in PersistenceContexts)
			{
				if (persistenceContextInfo.ContextName != contextName)
				{
					continue;
				}

				persistenceContextProvider = persistenceContextInfo.NewDatabaseContextProvider();
				this.databaseContextProviders[contextName] = persistenceContextProvider;

				return true;
			}

			return false;
		}
	}
}
