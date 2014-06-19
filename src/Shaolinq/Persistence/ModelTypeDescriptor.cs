// Copyright (c) 2007-2014 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Platform;
using Platform.Reflection;

namespace Shaolinq.Persistence
{
	public class ModelTypeDescriptor
	{
		public Type Type { get; private set; }
		public TypeDescriptorProvider TypeDescriptorProvider { get; private set; }
		public DataAccessModelAttribute DataAccessModelAttribute { get; private set; }
		private readonly Dictionary<Type, EnumTypeDescriptor> enumTypeDescriptors; 
		private readonly Dictionary<Type, TypeDescriptor> typeDescriptors = new Dictionary<Type, TypeDescriptor>();
		
		public ModelTypeDescriptor(TypeDescriptorProvider typeDescriptorProvider, Type type)
		{
			TypeDescriptorProvider = typeDescriptorProvider;
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

				var typeDescriptor = this.TypeDescriptorProvider.GetTypeDescriptor(genericType);

				if (typeDescriptor == null)
				{
					throw new InvalidDataAccessObjectModelDefinition(string.Format("DataAccessObject type '{0}' is missing [DataAccessObject] attribute", genericType.Name));
				}

				this.typeDescriptors[typeDescriptor.Type] = typeDescriptor;
			}

			var allEnumTypes = this.GetPersistedObjectTypeDescriptors()
			                       .SelectMany(c => c.PersistedProperties)
			                       .Where(c => (Nullable.GetUnderlyingType(c.PropertyType) ?? c.PropertyType).IsEnum)
								   .Select(c => (Nullable.GetUnderlyingType(c.PropertyType) ?? c.PropertyType));

			enumTypeDescriptors = Enumerable.Distinct(allEnumTypes).ToDictionary(c => c, c => new EnumTypeDescriptor(c));

			this.TypeDescriptorProvider.AddEnumTypeDescriptors(enumTypeDescriptors.Values);
		}

		public IEnumerable<EnumTypeDescriptor> GetPersistedEnumTypeDescriptors()
		{
			return enumTypeDescriptors.Values.Sorted((x, y) => String.Compare(x.Name, y.Name, StringComparison.Ordinal));
		}

		public IEnumerable<TypeDescriptor> GetPersistedObjectTypeDescriptors()
		{
			return this.typeDescriptors.Values.Sorted((x, y) => String.Compare(x.PersistedName, y.PersistedName, StringComparison.Ordinal));
		}

		public TypeDescriptor GetTypeDescriptor(Type type)
		{
			TypeDescriptor retval;

			if (!this.typeDescriptors.TryGetValue(type, out retval))
			{
				throw new InvalidDataAccessObjectModelDefinition(string.Format("{0} is missing a [DataAccessObjects] property for the type: {1}", this.Type.Name, type.Name));	
			}

			return retval;
		}
	}
}
