// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Reflection;
using Platform;
using Platform.Reflection;

namespace Shaolinq.Persistence
{
	public class ModelTypeDescriptor
	{
		public Type Type { get; private set; }
		public DataAccessModelAttribute DataAccessModelAttribute { get; private set; }
		
		private readonly Dictionary<Type, TypeDescriptor> typeDescriptors = new Dictionary<Type, TypeDescriptor>();
		
		public ModelTypeDescriptor(Type type)
		{
			this.Type = type;

			this.DataAccessModelAttribute = type.GetFirstCustomAttribute<DataAccessModelAttribute>(true);

			foreach (var propertyInfo in this.Type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
			{
				var queryableAttribute = propertyInfo.GetFirstCustomAttribute<DataAccessObjectsAttribute>(true);

				if (queryableAttribute == null)
				{
					continue;
				}

				var propertyType = propertyInfo.PropertyType;
				Type genericType = null;

				while (propertyType != null)
				{
					if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(DataAccessObjectsQueryable<>))
					{
						genericType = propertyType.GetGenericArguments()[0];
					}

					propertyType = propertyType.BaseType;
				}

				if (genericType == null)
				{
					throw new ArgumentException("The DataAccessObjects queryable has no generic type");
				}

				var provider = TypeDescriptorProvider.GetProvider(genericType.Assembly);

				var typeDescriptor = provider.GetTypeDescriptor(genericType);

				if (typeDescriptor == null)
				{
					throw new InvalidDataAccessObjectModelDefinition(string.Format("DataAccessObject type '{0}' is missing [DataAccessObject] attribute", genericType.Name));
				}

				this.typeDescriptors[typeDescriptor.Type] = typeDescriptor;
			}
		}

		public IEnumerable<Type> GetQueryableTypes()
		{
			return this.typeDescriptors.Keys;
		}

		public IEnumerable<TypeDescriptor> GetQueryableTypeDescriptors(DataAccessModel model)
		{
			return this.typeDescriptors.Values.Sorted((x, y) => String.Compare(x.GetPersistedName(model), y.GetPersistedName(model), System.StringComparison.Ordinal));
		}

		public TypeDescriptor GetQueryableTypeDescriptor(Type type)
		{
			TypeDescriptor retval;

			if (!this.typeDescriptors.TryGetValue(type, out retval))
			{
				throw new InvalidDataAccessObjectModelDefinition(string.Format("{0} is missing a [DataAccessObjects] property for the type: {1}", this.Type.Name, type.Name));	
			}

			return retval;
		}

		public IEnumerable<TypeDescriptor> GetQueryableTypeDescriptors(DataAccessModel model, Predicate<TypeDescriptor> accept)
		{
			return this.typeDescriptors.Values.Filter(accept);
		}
	}
}
