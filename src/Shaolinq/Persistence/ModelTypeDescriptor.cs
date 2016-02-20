// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Reflection;
using Platform;
using Platform.Reflection;

namespace Shaolinq.Persistence
{
	public class ModelTypeDescriptor
	{
		public Type Type { get; }
		public TypeDescriptorProvider TypeDescriptorProvider { get; }
		public DataAccessModelAttribute DataAccessModelAttribute { get; }
		
		public ModelTypeDescriptor(TypeDescriptorProvider typeDescriptorProvider, Type type)
		{
			this.TypeDescriptorProvider = typeDescriptorProvider;
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
					if (propertyType.GetGenericTypeDefinitionOrNull() == typeof(DataAccessObjectsQueryable<>))
					{
						genericType = propertyType.GetGenericArguments()[0];
					}

					propertyType = propertyType.BaseType;
				}

				if (genericType == null)
				{
					throw new ArgumentException("The DataAccessObjects queryable has no generic type");
				}

				var typeDescriptor = this.TypeDescriptorProvider.GetTypeDescriptor(genericType);

				if (typeDescriptor == null)
				{
					throw new InvalidDataAccessObjectModelDefinition($"Type {genericType.Name} is referenced by model but not resolvable");
				}
			}
		}
	}
}
