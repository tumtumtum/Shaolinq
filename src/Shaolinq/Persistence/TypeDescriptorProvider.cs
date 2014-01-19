// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Shaolinq.TypeBuilding;
using Platform;
using Platform.Reflection;

namespace Shaolinq.Persistence
{
	public class TypeDescriptorProvider
	{
		private static IDictionary<Assembly, TypeDescriptorProvider> typeDescriptorProvidersByAssembly = new Dictionary<Assembly, TypeDescriptorProvider>(PrimeNumbers.Prime7);
		
		public Assembly Assembly { get; private set; }
		private readonly List<Type> dataAccessModelTypes = new List<Type>();
		private Dictionary<Type, EnumTypeDescriptor> enumTypeDescriptorsByType = new Dictionary<Type,EnumTypeDescriptor>();
		private Dictionary<Type, ModelTypeDescriptor> modelTypeDescriptorsByType = new Dictionary<Type, ModelTypeDescriptor>();
		private readonly Dictionary<Type, TypeDescriptor> typeDescriptorsByType = new Dictionary<Type, TypeDescriptor>();
		
		public static TypeDescriptorProvider GetProvider(Assembly assembly)
		{
			TypeDescriptorProvider retval;

			assembly = DataAccessModelAssemblyBuilder.Default.GetDefinitionAssembly(assembly);

			if (typeDescriptorProvidersByAssembly.TryGetValue(assembly, out retval))
			{
				return retval;
			}

			lock (typeof(TypeDescriptorProvider))
			{
				if (!typeDescriptorProvidersByAssembly.TryGetValue(assembly, out retval))
				{
					retval = new TypeDescriptorProvider(assembly);

					var newTypeDescriptorProvidersByAssembly = new Dictionary<Assembly, TypeDescriptorProvider>(PrimeNumbers.Prime7);

					foreach (var kvp in typeDescriptorProvidersByAssembly)
					{
						newTypeDescriptorProvidersByAssembly[kvp.Key] = kvp.Value;
					}

					newTypeDescriptorProvidersByAssembly[assembly] = retval;
					typeDescriptorProvidersByAssembly = newTypeDescriptorProvidersByAssembly;
				}

				return retval;
			}
		}

		private TypeDescriptorProvider(Assembly assembly)
		{
			this.Assembly = assembly;

			this.Process();
		}

		private void Process()
		{
			foreach (var type in this.Assembly.GetTypes())
			{
				var dataAccessModelAttribute = type.GetFirstCustomAttribute<DataAccessModelAttribute>(true);

				if (dataAccessModelAttribute != null)
				{
					dataAccessModelTypes.Add(type);
				}

				if (typeof(DataAccessModel).IsAssignableFrom(type) && dataAccessModelAttribute == null)
				{
					throw new InvalidDataAccessObjectModelDefinition("The DataAccessModel type '{0}' is missing a [DataAccessModel] attribute", type.Name);
				}

				var baseType = type;
				
				while (baseType != null)
				{
					var dataAccessObjectAttribute = baseType.GetFirstCustomAttribute<DataAccessObjectAttribute>(false);

					if (dataAccessModelAttribute != null && dataAccessObjectAttribute != null)
					{
						throw new InvalidDataAccessObjectModelDefinition("The type '{0}' cannot be decorated with both a [DataAccessModel] attribute and a DataAccessObjectAttribute", baseType.Name);
					}

					if (dataAccessObjectAttribute != null)
					{
						if (!typeDescriptorsByType.ContainsKey(baseType) && !dataAccessObjectAttribute.NotPersisted)
						{
							if (!typeof(IDataAccessObject).IsAssignableFrom(baseType))
							{
								throw new InvalidDataAccessObjectModelDefinition("The type {0} is decorated with a [DataAccessObject] attribute but does not extend DataAccessObject<T>", baseType.Name);
							}

							var typeDescriptor = new TypeDescriptor(this, baseType);

							if (typeDescriptor.PrimaryKeyProperties.Count(c => c.PropertyType.IsNullableType()) > 0)
							{
								throw new InvalidDataAccessObjectModelDefinition("The type {0} illegally defines a nullable primary key", baseType.Name);
							}

							typeDescriptorsByType[baseType] = typeDescriptor;
						}
					}
					else if (typeof(IDataAccessObject).IsAssignableFrom(baseType))
					{
						throw new InvalidDataAccessObjectModelDefinition("The type {0} extends DataAccessObject<T> but is not explicitly decorated with the [DataAccessObject] attribute", baseType.Name);
					}

					baseType = baseType.BaseType;
				}
			}
			
			// Resolve relationships

			foreach (var typeDescriptor in typeDescriptorsByType.Values)
			{
				foreach (var propertyDescriptor in typeDescriptor.RelatedProperties)
				{
					if (typeof(RelatedDataAccessObjects<>).IsAssignableFromIgnoreGenericParameters(propertyDescriptor.PropertyType))
					{
						var currentType = propertyDescriptor.PropertyType;

						while (!currentType.IsGenericType || (currentType.IsGenericType && currentType.GetGenericTypeDefinition() != typeof(RelatedDataAccessObjects<>)))
						{
							currentType = currentType.BaseType;
						}

						var relatedType = currentType.GetGenericArguments()[0];

						var relatedTypeDescriptor = this.typeDescriptorsByType[relatedType];
						var typeRelationshipInfo = typeDescriptor.GetRelationshipInfo(relatedTypeDescriptor);

						if (typeRelationshipInfo != null)
						{
							if (typeRelationshipInfo.RelatedTypeTypeDescriptor != relatedTypeDescriptor)
							{
								throw new InvalidDataAccessObjectModelDefinition("The type {0} defines multiple relationships with the type {1}", typeDescriptor.Type.Name, relatedTypeDescriptor.Type.Name);
							}

							typeRelationshipInfo.EntityRelationshipType = EntityRelationshipType.ManyToMany;
							typeRelationshipInfo.RelatedTypeTypeDescriptor = relatedTypeDescriptor;
							relatedTypeDescriptor.SetOrCreateRelationshipInfo(typeDescriptor, EntityRelationshipType.ManyToMany, null);
						}
						else
						{
							var relatedProperty = relatedTypeDescriptor.GetRelatedProperty(typeDescriptor.Type);
							relatedTypeDescriptor.SetOrCreateRelationshipInfo(typeDescriptor, EntityRelationshipType.ChildOfOneToMany, relatedProperty);
						}
					}
				}
			}

			foreach (var typeDescriptor in this.typeDescriptorsByType.Values)
			{
				foreach (var relationshipInfo in typeDescriptor.GetRelationshipInfos())
				{
					var closedCelationshipInfo = relationshipInfo;

					if (relationshipInfo.EntityRelationshipType == EntityRelationshipType.ChildOfOneToMany)
					{
						if (!typeDescriptor.RelatedProperties.Filter(c => c.BackReferenceAttribute != null && c.PropertyType == closedCelationshipInfo.ReferencingProperty.PropertyType).Any())
						{
							throw new InvalidDataAccessObjectModelDefinition("The child type {0} participates in a one-many relationship with the parent type {1} but does not explicitly define a BackReference property", typeDescriptor, relationshipInfo.ReferencingProperty.DeclaringTypeDescriptor);
						}
					}
				}
			}

			foreach (var modelTypeDescriptor in this.dataAccessModelTypes.Select(dataAccessModelType => new ModelTypeDescriptor(this, dataAccessModelType)))
			{
				this.modelTypeDescriptorsByType[modelTypeDescriptor.Type] = modelTypeDescriptor;
			}
		}

		public IEnumerable<Type> GetEnumTypes()
		{
			return enumTypeDescriptorsByType.Keys;
		}

		public IEnumerable<EnumTypeDescriptor> GetEnumTypeDescriptors()
		{
			return enumTypeDescriptorsByType.Values;
		}

		public EnumTypeDescriptor GetEnumTypeDescriptor(Type type)
		{
			EnumTypeDescriptor retval;

			if (enumTypeDescriptorsByType.TryGetValue(type, out retval))
			{
				return retval;
			}

			return null;
		}

		public IEnumerable<Type> GetTypes()
		{
			return typeDescriptorsByType.Keys;
		}

		public IEnumerable<TypeDescriptor> GetTypeDescriptors()
		{
			return typeDescriptorsByType.Values;
		}

		public TypeDescriptor GetTypeDescriptor(Type type)
		{
			TypeDescriptor retval;

			if (typeDescriptorsByType.TryGetValue(type, out retval))
			{
				return retval;
			}

			return null;
		}

		public IEnumerable<ModelTypeDescriptor> GetModelTypeDescriptors()
		{
			return this.modelTypeDescriptorsByType.Values;
		}

		public ModelTypeDescriptor GetModelTypeDescriptor(Type type)
		{
			ModelTypeDescriptor retval;

			if (modelTypeDescriptorsByType.TryGetValue(type, out retval))
			{
				return retval;
			}

			return null;
		}

		internal void AddEnumTypeDescriptors(IEnumerable<EnumTypeDescriptor> values)
		{
			foreach (var value in values)
			{
				this.enumTypeDescriptorsByType[value.EnumType] = value;
			}
		}
	}
}
