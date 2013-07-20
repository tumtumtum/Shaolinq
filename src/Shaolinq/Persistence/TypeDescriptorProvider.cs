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
		private static IDictionary<Assembly, TypeDescriptorProvider> c_TypeDescriptorProvidersByAssembly = new Dictionary<Assembly, TypeDescriptorProvider>(PrimeNumbers.Prime7);

		public static TypeDescriptorProvider GetProvider(Assembly assembly)
		{
			TypeDescriptorProvider retval;

			assembly = DataAccessModelAssemblyBuilder.Default.GetDefinitionAssembly(assembly);

			if (c_TypeDescriptorProvidersByAssembly.TryGetValue(assembly, out retval))
			{
				return retval;
			}

			lock (typeof(TypeDescriptorProvider))
			{
				if (!c_TypeDescriptorProvidersByAssembly.TryGetValue(assembly, out retval))
				{
					retval = new TypeDescriptorProvider(assembly);

					var newTypeDescriptorProvidersByAssembly = new Dictionary<Assembly, TypeDescriptorProvider>(PrimeNumbers.Prime7);

					foreach (var kvp in c_TypeDescriptorProvidersByAssembly)
					{
						newTypeDescriptorProvidersByAssembly[kvp.Key] = kvp.Value;
					}

					newTypeDescriptorProvidersByAssembly[assembly] = retval;
					c_TypeDescriptorProvidersByAssembly = newTypeDescriptorProvidersByAssembly;
				}

				return retval;
			}
		}

		private Dictionary<Type, ModelTypeDescriptor> modelTypeDescriptorsByType;
		private readonly Dictionary<Type, TypeDescriptor> typeDescriptorsByType = new Dictionary<Type, TypeDescriptor>();

		public Assembly Assembly { get; private set; }

		private TypeDescriptorProvider(Assembly assembly)
		{
			this.Assembly = assembly;

			Parse();
		}

		private readonly List<Type> dataAccessModelTypes = new List<Type>();

		private void Parse()
		{
			foreach (Type type in this.Assembly.GetTypes())
			{
				var dataAccessModelAttribute = type.GetFirstCustomAttribute<DataAccessModelAttribute>(true);

				if (dataAccessModelAttribute != null)
				{
					dataAccessModelTypes.Add(type);
				}

				if (typeof(BaseDataAccessModel).IsAssignableFrom(type) && dataAccessModelAttribute == null)
				{
					throw new InvalidDataAccessObjectModelDefinition("The BaseDataAccessModel type '{0}' is missing a DataAccessModelAttribute", type.Name);
				}

				var baseType = type;

				while (baseType != null)
				{
					var dataAccessObjectAttribute = baseType.GetFirstCustomAttribute<DataAccessObjectAttribute>(true);

					if (dataAccessModelAttribute != null && dataAccessObjectAttribute != null)
					{
						throw new InvalidDataAccessObjectModelDefinition("The type '{0}' cannot be decorated with both a DataAccessModelAttribute and a DataAccessObjectAttribute", baseType.Name);
					}

					if (dataAccessObjectAttribute != null)
					{
						if (!typeDescriptorsByType.ContainsKey(baseType) && !dataAccessObjectAttribute.Abstract)
						{
							if (!typeof(IDataAccessObject).IsAssignableFrom(baseType))
							{
								throw new InvalidDataAccessObjectModelDefinition("The type {0} is decorated with a DataAccessObject attribute but does not inherit from DataAccessObject<OBJECT_TYPE>", baseType.Name);
							}

							var typeDescriptor = new TypeDescriptor(baseType);

							typeDescriptorsByType[baseType] = typeDescriptor;
						}
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
						TypeDescriptor relatedTypeDescriptor;
						TypeRelationshipInfo typeRelationshipInfo;

						var currentType = propertyDescriptor.PropertyType;

						while (!currentType.IsGenericType || (currentType.IsGenericType && currentType.GetGenericTypeDefinition() != typeof(RelatedDataAccessObjects<>)))
						{
							currentType = currentType.BaseType;
						}

						var relatedType = currentType.GetGenericArguments()[0];

						relatedTypeDescriptor = typeDescriptorsByType[relatedType];
						typeRelationshipInfo = typeDescriptor.GetRelationshipInfo(relatedTypeDescriptor);

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
						if (!typeDescriptor.RelatedProperties.Filter(c => c.BackReferenceAttribute != null && c.PropertyType == closedCelationshipInfo.RelatedProperty.PropertyType).Any())
						{
							throw new InvalidDataAccessObjectModelDefinition("The child type {0} participates in a one-many relationship with the parent type {1} but does not explicitly define a BackReference property", typeDescriptor, relationshipInfo.RelatedProperty.DeclaringTypeDescriptor);
						}
					}
				}
			}
		}

		private void BuildModelTypeDescriptors()
		{
			// Resolve model type descriptors

			modelTypeDescriptorsByType = new Dictionary<Type, ModelTypeDescriptor>();

			foreach (var dataAccessModelType in dataAccessModelTypes)
			{
				var modelTypeDescriptor = new ModelTypeDescriptor(dataAccessModelType);

				modelTypeDescriptorsByType[modelTypeDescriptor.Type] = modelTypeDescriptor;
			}
		}

		public IEnumerable<Type> GetTypes()
		{
			return typeDescriptorsByType.Keys;
		}

		public IEnumerable<Type> GetTypes(Predicate<Type> accept)
		{
			return typeDescriptorsByType.Keys.Filter(accept);
		}

		public IEnumerable<TypeDescriptor> GetTypeDescriptors()
		{
			return typeDescriptorsByType.Values;
		}

		public IEnumerable<TypeDescriptor> GetTypeDescriptors(Predicate<TypeDescriptor> accept)
		{
			return typeDescriptorsByType.Values.Filter(accept);
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
			if (modelTypeDescriptorsByType == null)
			{
				BuildModelTypeDescriptors();
			}

			return this.modelTypeDescriptorsByType.Values;
		}

		public ModelTypeDescriptor GetModelTypeDescriptor(Type type)
		{
			ModelTypeDescriptor retval;

			if (modelTypeDescriptorsByType == null)
			{
				BuildModelTypeDescriptors();
			}

			if (modelTypeDescriptorsByType.TryGetValue(type, out retval))
			{
				return retval;
			}

			return null;
		}
	}
}