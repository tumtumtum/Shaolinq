// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Platform;
using Platform.Reflection;

namespace Shaolinq.Persistence
{
	public class TypeDescriptorProvider
	{
		public Type DataAccessModelType { get; }
		public ModelTypeDescriptor ModelTypeDescriptor { get; }
		public DataAccessModelConfiguration Configuration { get; }

		private readonly Dictionary<Type, EnumTypeDescriptor> enumTypeDescriptorsByType;
		private readonly Dictionary<Type, TypeDescriptor> typeDescriptorsByType = new Dictionary<Type, TypeDescriptor>();
		
		public TypeDescriptorProvider(Type dataAccessModelType, DataAccessModelConfiguration configuration)
		{
			this.Configuration = configuration;
			this.DataAccessModelType = dataAccessModelType;

			var dataAccessModelAttribute = dataAccessModelType.GetFirstCustomAttribute<DataAccessModelAttribute>(true);

			if (typeof(DataAccessModel).IsAssignableFrom(dataAccessModelType) && dataAccessModelAttribute == null)
			{
				throw new InvalidDataAccessObjectModelDefinition("The DataAccessModel type '{0}' is missing a DataAccessModelAttribute", dataAccessModelType.Name);
			}

			foreach (var type in this.DataAccessModelType
				.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy)
				.Where(c => c.PropertyType.GetGenericTypeDefinitionOrNull() == typeof(DataAccessObjects<>))
				.Select(c => c.PropertyType.GetGenericArguments()[0]))
			{
				var currentType = type;

				while (currentType != null 
					&& currentType != typeof(DataAccessObject) 
					&& !(currentType.GetGenericTypeDefinitionOrNull() == typeof(DataAccessObject<>)))
				{
					var dataAccessObjectAttribute = currentType.GetFirstCustomAttribute<DataAccessObjectAttribute>(false);

					if (dataAccessObjectAttribute != null)
					{
						if (!this.typeDescriptorsByType.ContainsKey(currentType))
						{
							if (!typeof(DataAccessObject).IsAssignableFrom(currentType))
							{
								throw new InvalidDataAccessObjectModelDefinition("The type {0} is decorated with a [DataAccessObject] attribute but does not extend DataAccessObject<T>", currentType.Name);
							}

							var typeDescriptor = new TypeDescriptor(this, currentType);

							if (typeDescriptor.PrimaryKeyProperties.Count(c => c.PropertyType.IsNullableType()) > 0)
							{
								throw new InvalidDataAccessObjectModelDefinition("The type {0} illegally defines a nullable primary key", currentType.Name);
							}

							this.typeDescriptorsByType[currentType] = typeDescriptor;
						}
					}
					else
					{
						throw new InvalidDataAccessObjectModelDefinition("Type '{0}' does not have a DataAccessObject attribute", currentType);
					}

					currentType = currentType.BaseType;
				}
			}

			var typesSet = new HashSet<Type>(this.typeDescriptorsByType.Keys);

			var typesReferenced = this.typeDescriptorsByType
				.Values
				.SelectMany(c => c.PersistedProperties)
				.Select(c => c.PropertyType)
				.Where(c => typeof(DataAccessObject).IsAssignableFrom(c))
				.Distinct();

			var first = typesReferenced.FirstOrDefault(c => !typesSet.Contains(c));

			if (first != null)
			{
				throw new InvalidDataAccessModelDefinitionException($"Type {first.Name} is referenced but is not declared as a property {dataAccessModelType.Name}");
			}	

			// Enums

			this.enumTypeDescriptorsByType = this.typeDescriptorsByType
				.Values
				.SelectMany(c => c.PersistedProperties)
				.Select(c => c.PropertyType.GetUnwrappedNullableType())
				.Where(c => c.IsEnum)
				.Distinct()
				.Select(c => new EnumTypeDescriptor(c))
				.ToDictionary(c => c.EnumType, c => c);

			// Resolve relationships

			foreach (var typeDescriptor in this.typeDescriptorsByType.Values)
			{
				foreach (var propertyDescriptor in typeDescriptor.RelationshipRelatedProperties.Where(c => c.IsRelatedDataAccessObjectsProperty))
				{
					if (typeof(RelatedDataAccessObjects<>).IsAssignableFromIgnoreGenericParameters(propertyDescriptor.PropertyType))
					{
						var currentType = propertyDescriptor.PropertyType;
						
						while (currentType != null && currentType.GetGenericTypeDefinitionOrNull() != typeof(RelatedDataAccessObjects<>))
						{
							currentType = currentType?.BaseType;
						}

						if (currentType == null)
						{
							throw new InvalidOperationException("Code should be unreachable");
						}

						var relatedTypeDescriptor = this.typeDescriptorsByType[currentType.GetSequenceElementType()];

						var relatedProperty = relatedTypeDescriptor
							.RelationshipRelatedProperties
							.Where(c => c.IsBackReferenceProperty)
							.SingleOrDefault(c => c.PropertyName == propertyDescriptor.RelatedDataAccessObjectsAttribute.BackReferenceName);

						relatedProperty = relatedProperty ?? relatedTypeDescriptor
							.RelationshipRelatedProperties
							.Where(c => c.IsBackReferenceProperty)
							.SingleOrDefault(c => typeDescriptor.Type == c.PropertyType);

						relatedProperty = relatedProperty ?? relatedTypeDescriptor
							.RelationshipRelatedProperties
							.Where(c => c.IsBackReferenceProperty)
							.SingleOrDefault(c => typeDescriptor.Type.IsAssignableFrom(c.PropertyType));

						typeDescriptor.AddRelationshipInfo(RelationshipType.ParentOfOneToMany, propertyDescriptor, relatedProperty);
						relatedTypeDescriptor.AddRelationshipInfo(RelationshipType.ChildOfOneToMany, relatedProperty, propertyDescriptor);
					}
				}
			}

			this.ModelTypeDescriptor = new ModelTypeDescriptor(this, dataAccessModelType);
		}

		public EnumTypeDescriptor GetEnumTypeDescriptor(Type type)
		{
			EnumTypeDescriptor retval;

			return this.enumTypeDescriptorsByType.TryGetValue(type, out retval) ? retval : null;
		}

		public ICollection<TypeDescriptor> GetTypeDescriptors()
		{
			return this.typeDescriptorsByType.Values;
		}

		public TypeDescriptor GetTypeDescriptor(Type type)
		{
			TypeDescriptor retval;

			return this.typeDescriptorsByType.TryGetValue(type, out retval) ? retval : null;
		}

		public IEnumerable<EnumTypeDescriptor> GetPersistedEnumTypeDescriptors()
		{
			return this.enumTypeDescriptorsByType
				.Values
				.Sorted((x, y) => string.Compare(x.Name, y.Name, StringComparison.Ordinal));
		}

		public IEnumerable<TypeDescriptor> GetPersistedObjectTypeDescriptors()
		{
			return this.typeDescriptorsByType
				.Values
				.Where(c => !c.DataAccessObjectAttribute.NotPersisted)
				.Sorted((x, y) => string.Compare(x.PersistedName, y.PersistedName, StringComparison.Ordinal));
		}
	}
}
