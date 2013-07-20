using System;
using System.Collections.Generic;
using System.Reflection;
using Platform;
using Platform.Reflection;

namespace Shaolinq.Persistence
{
	public class ModelTypeDescriptor
	{
		private readonly Dictionary<Type, TypeDescriptor> typeDescriptors = new Dictionary<Type, TypeDescriptor>();
		private readonly Dictionary<Type, PersistenceContextAttribute> typePersistenceContexts = new Dictionary<Type, PersistenceContextAttribute>();

		public ModelTypeDescriptor(Type type)
		{
			this.Type = type;

			this.DataAccessModelAttribute = type.GetFirstCustomAttribute<DataAccessModelAttribute>(true);

			this.PersistenceContextAttribute = type.GetFirstCustomAttribute<PersistenceContextAttribute>(true) ?? PersistenceContextAttribute.Default;

			foreach (var propertyInfo in this.Type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
			{
				var queryableAttribute = propertyInfo.GetFirstCustomAttribute<DataAccessObjectsAttribute>(true);

				if (queryableAttribute == null)
				{
					continue;
				}

				// Use the persistance context attribute on the property if it exists
				var propertyPersistenceContextAttribute = propertyInfo.GetFirstCustomAttribute<PersistenceContextAttribute>(true);

				Type t = propertyInfo.PropertyType;
				Type genericType = null;

				while (t != null)
				{
					if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(DataAccessObjectsQueryable<>))
					{
						genericType = t.GetGenericArguments()[0];
					}

					t = t.BaseType;
				}

				if (genericType == null)
				{
					throw new ArgumentException("The DataAccessObjects queryable has no generic type");
				}

				var provider = TypeDescriptorProvider.GetProvider(genericType.Assembly);

				var typeDescriptor = provider.GetTypeDescriptor(genericType);
				
				this.typeDescriptors[typeDescriptor.Type] = typeDescriptor;

				var persistenceContextAttribute = propertyPersistenceContextAttribute ?? typeDescriptor.PersistenceContextAttribute;

				this.typePersistenceContexts[typeDescriptor.Type] = persistenceContextAttribute;
			}
		}

		public Type Type
		{
			get;
			private set;
		}

		public DataAccessModelAttribute DataAccessModelAttribute
		{
			get;
			private set;
		}

		public PersistenceContextAttribute PersistenceContextAttribute
		{
			get;
			private set;
		}

		public IEnumerable<Type> GetQueryableTypes()
		{
			return this.typeDescriptors.Keys;
		}

		public IEnumerable<TypeDescriptor> GetQueryableTypeDescriptors()
		{
			return this.typeDescriptors.Values;
		}

		public IEnumerable<TypeDescriptor> GetQueryableTypeDescriptors(BaseDataAccessModel model, string contextName)
		{
			return this.GetQueryableTypeDescriptors(model, t => this.GetQueryablePersistenceContextName(t.Type) == contextName).Sorted((x, y) => x.GetPersistedName(model).CompareTo(y.GetPersistedName(model)));
		}

		public TypeDescriptor GetQueryableTypeDescriptor(Type type)
		{
			return this.typeDescriptors[type];
		}

		public IEnumerable<TypeDescriptor> GetQueryableTypeDescriptors(BaseDataAccessModel model, Predicate<TypeDescriptor> accept)
		{
			return this.typeDescriptors.Values.Filter(accept);
		}

		public PersistenceContextAttribute GetQueryablePersistenceContextAttribute(Type type)
		{
			return this.typePersistenceContexts[type];
		}

		public string GetQueryablePersistenceContextName(Type type)
		{
			return this.typePersistenceContexts[type].GetPersistenceContextName(type);
		}
	}
}