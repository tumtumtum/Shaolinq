// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Platform;
using Platform.Reflection;
using Shaolinq.Persistence.Computed;

namespace Shaolinq.Persistence
{
	public class TypeDescriptor
	{
		public Type Type { get; }
		public string PersistedName { get; }
		public TypeDescriptorProvider TypeDescriptorProvider { get; }
		public DataAccessObjectAttribute DataAccessObjectAttribute { get; }
		public IReadOnlyList<PropertyDescriptor> ComputedProperties { get; private set; }
		public IReadOnlyList<PropertyDescriptor> PersistedPropertiesWithoutBackreferences { get; private set;}
		public IReadOnlyList<PropertyDescriptor> PrimaryKeyProperties { get; private set;}
		public IReadOnlyList<PropertyDescriptor> ComputedTextProperties { get; private set; }
		public IReadOnlyList<PropertyDescriptor> RelationshipRelatedProperties { get; private set; }
		public IReadOnlyList<PropertyDescriptor> PersistedProperties { get; private set;}
		public IReadOnlyList<PropertyDescriptor> PrimaryKeyDerivableProperties { get; private set; }
		public IReadOnlyList<IndexAttribute> IndexAttributes { get; private set; }
		public OrganizationIndexAttribute OrganizationIndexAttribute { get; set; }

		public string TypeName => this.Type.Name;
		public int PrimaryKeyCount => this.PrimaryKeyProperties.Count;
		public bool HasPrimaryKeys => this.PrimaryKeyProperties.Count > 0;
		
		private List<TypeRelationshipInfo> relationshipInfos;
		internal IDictionary<string, PropertyDescriptor> propertyDescriptorByColumnName;
		private IDictionary<string, PropertyDescriptor> propertyDescriptorByPropertyName;
		private readonly Dictionary<Type, PropertyDescriptor> relatedPropertiesByType = new Dictionary<Type, PropertyDescriptor>();

		public override string ToString() => "TypeDescriptor: " + this.Type.Name;

		public static bool IsSimpleType(Type type)
		{
			if (type.IsPrimitive)
			{
				return true;
			}

			if (type.IsValueType)
			{
				return true;
			}

			if (type == typeof(string))
			{
				return true;
			}

			return false;
		}
		
		public Expression GetSinglePrimaryKeyExpression(Expression obj)
		{
			if (this.PrimaryKeyProperties.Count != 1)
			{
				return null;
			}

			return Expression.Property(obj, this.PrimaryKeyProperties[0].PropertyInfo);
		}

		public IEnumerable<TypeRelationshipInfo> GetRelationshipInfos()
		{
			return this.relationshipInfos;
		}
		
		public PropertyDescriptor GetPropertyDescriptorByColumnName(string columnName)
		{
			return !this.propertyDescriptorByColumnName.TryGetValue(columnName, out var retval) ? null : retval;
		}

		public PropertyDescriptor GetPropertyDescriptorByPropertyName(string propertyName)
		{
			return !this.propertyDescriptorByPropertyName.TryGetValue(propertyName, out var retval) ? null : retval;
		}

		public PropertyDescriptor GetRelatedProperty(Type type)
		{

			if (!this.relatedPropertiesByType.TryGetValue(type, out var retval))
			{
				bool IsForType(PropertyDescriptor c)
				{
					if (type.IsAssignableFrom(c.PropertyType))
					{
						return true;
					}

					if (c.PropertyType.IsGenericType && typeof(RelatedDataAccessObjects<>).IsAssignableFromIgnoreGenericParameters(c.PropertyType.GetGenericTypeDefinition()))
					{
						if (c.PropertyType.GetGenericArguments()[0] == type)
						{
							return true;
						}
					}

					return false;
				}

				retval = this.RelationshipRelatedProperties.FirstOrDefault(IsForType);

				this.relatedPropertiesByType[type] = retval ?? throw new InvalidOperationException($"Unable to find related property for type '{type.Name}' on type '{this.Type.Name}'");
			}

			return retval;
		}

		private static bool IsValidDataType(Type type)
		{
			var underlyingType = Nullable.GetUnderlyingType(type);

			if (underlyingType != null)
			{
				return IsValidDataType(underlyingType);
			}

			if (type.IsIntegralType()
				|| type.IsEnum
				|| type.IsDataAccessObjectType())
			{
				return true;
			}

			return type.GetConversionMembers().Any(c => IsValidDataType(c.GetMemberReturnType()));
		}

		private IEnumerable<PropertyInfo> GetPropertiesInOrder()
		{
			var baseType = this.Type;
			var declaringTypes = new Stack<Type>();

			while (baseType != null)
			{
				declaringTypes.Push(baseType);

				baseType = baseType.BaseType;
			}

			while (declaringTypes.Count > 0)
			{
				var declaringType = declaringTypes.Pop();

				foreach (var propertyInfo in declaringType.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
				{
					yield return this.Type.GetProperties().First(c => c.Name == propertyInfo.Name);
				}
			}
		}

		internal void AddRelationshipInfo(RelationshipType relationshipType, PropertyDescriptor relatingProperty, PropertyDescriptor targetProperty)
		{
			this.relationshipInfos.Add(new TypeRelationshipInfo(relationshipType, relatingProperty, targetProperty));
		}

		public TypeDescriptor(TypeDescriptorProvider typeDescriptorProvider, Type type)
		{
			this.Type = type;
			this.TypeDescriptorProvider = typeDescriptorProvider;
			this.DataAccessObjectAttribute = type.GetFirstCustomAttribute<DataAccessObjectAttribute>(true);
			this.PersistedName = this.DataAccessObjectAttribute.GetName(this, this.TypeDescriptorProvider.Configuration.NamingTransforms?.DataAccessObjectName);
			
			
		}

		internal void Complete()
		{
			var propertyDescriptorsInOrder = new List<PropertyDescriptor>();
			
			var relatedProperties = new List<PropertyDescriptor>();
			this.relationshipInfos = new List<TypeRelationshipInfo>();
			this.propertyDescriptorByColumnName = new Dictionary<string, PropertyDescriptor>();
			this.propertyDescriptorByPropertyName = new Dictionary<string, PropertyDescriptor>();

			var alreadyEnteredProperties = new HashSet<string>();
			foreach (var propertyInfo in GetPropertiesInOrder())
			{
				if (alreadyEnteredProperties.Contains(propertyInfo.Name))
				{
					continue;
				}

				alreadyEnteredProperties.Add(propertyInfo.Name);

				var attribute = (PersistedMemberAttribute)propertyInfo.GetCustomAttributes().FirstOrDefault(c => c is PersistedMemberAttribute);

				if (attribute != null)
				{
					var propertyDescriptor = new PropertyDescriptor(this, this.Type, propertyInfo);

					if (propertyInfo.GetGetMethod() == null)
					{
						throw new InvalidDataAccessObjectModelDefinition("The property {0} is missing a required getter method", propertyInfo.Name);
					}

					if (propertyInfo.GetSetMethod() == null && !propertyDescriptor.IsComputedTextMember && !propertyDescriptor.IsComputedMember)
					{
						throw new InvalidDataAccessObjectModelDefinition("The property {0} is missing a required setter method", propertyInfo.Name);
					}

					if (!IsValidDataType(propertyInfo.PropertyType))
					{
						throw new InvalidDataAccessObjectModelDefinition("The property {0} cannot have a return type of {1}", propertyInfo.Name, propertyInfo.PropertyType.Name);
					}
					
					if (!(propertyInfo.GetGetMethod().IsAbstract || propertyInfo.GetGetMethod().IsVirtual))
					{
						throw new InvalidDataAccessObjectModelDefinition("The property {0} on {1} is not virtual or abstract", propertyInfo.Name, this.Type.Name);
					}

					propertyDescriptorsInOrder.Add(propertyDescriptor);

					this.propertyDescriptorByPropertyName[propertyInfo.Name] = propertyDescriptor;

					if (propertyInfo.GetFirstCustomAttribute<BackReferenceAttribute>(true) != null)
					{
						if (!propertyInfo.PropertyType.IsDataAccessObjectType())
						{
							throw new InvalidDataAccessObjectModelDefinition("The property {0} on {1} is decorated with a BackReference attribute but does not return a type that extends DataAccessObject<OBJECT_TYPE>", propertyInfo.Name, this.Type.Name);
						}

						if (propertyInfo.GetGetMethod() == null)
						{
							throw new InvalidDataAccessObjectModelDefinition("The property {0} is missing a required getter method", propertyInfo.Name);
						}

						if (propertyInfo.GetSetMethod() == null)
						{
							throw new InvalidDataAccessObjectModelDefinition("The property {0} is missing a required setter method", propertyInfo.Name);
						}

						if (!(propertyInfo.GetGetMethod().IsAbstract || propertyInfo.GetGetMethod().IsVirtual))
						{
							throw new InvalidDataAccessObjectModelDefinition("The property {0} on {1} is not virtual or abstract", propertyInfo.Name, this.Type.Name);
						}

						relatedProperties.Add(propertyDescriptor);
					}
				}
			}

			foreach (var propertyInfo in this.Type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
			{
				if (propertyInfo.GetFirstCustomAttribute<DataAccessObjectsAttribute>(true) != null)
				{
					throw new InvalidDataAccessObjectModelDefinition("The property {0} on {1} is decorated with a DataAccessObjects attribute.  Did you mean to you use RelatedDataAccessObjects attribute?", propertyInfo.Name, this.Type.Name);
				}

				if (propertyInfo.GetFirstCustomAttribute<RelatedDataAccessObjectsAttribute>(true) != null)
				{
					if (!typeof(RelatedDataAccessObjects<>).IsAssignableFromIgnoreGenericParameters(propertyInfo.PropertyType.GetGenericTypeDefinition()))
					{
						throw new InvalidDataAccessObjectModelDefinition("The property {0} on {1} is decorated with a RelatedDataAccessObjectsAttribute but the property type does not extend RelatedDataAccessObjects<T>", this.Type.Name, propertyInfo.Name);
					}

					if (propertyInfo.GetGetMethod() == null)
					{
						throw new InvalidDataAccessObjectModelDefinition("The property {0} is missing a required getter method", propertyInfo.Name);
					}

					if (!(propertyInfo.GetGetMethod().IsAbstract || propertyInfo.GetGetMethod().IsVirtual))
					{
						throw new InvalidDataAccessObjectModelDefinition("The property {0} on {1} is not virtual or abstract", propertyInfo.Name, this.Type.Name);
					}

					var propertyDescriptor = new PropertyDescriptor(this, this.Type, propertyInfo);

					relatedProperties.Add(propertyDescriptor);

					this.propertyDescriptorByPropertyName[propertyInfo.Name] = propertyDescriptor;
				}
			}

			this.PersistedProperties = propertyDescriptorsInOrder;
			this.RelationshipRelatedProperties = relatedProperties.ToReadOnlyCollection();
			this.PersistedPropertiesWithoutBackreferences = this.PersistedProperties.Where(c => !c.IsBackReferenceProperty).ToReadOnlyCollection();
			this.PrimaryKeyProperties = this.PersistedPropertiesWithoutBackreferences.Where(propertyDescriptor => propertyDescriptor.IsPrimaryKey).ToReadOnlyCollection();
			this.ComputedTextProperties = this.PersistedPropertiesWithoutBackreferences.Where(c => c.IsComputedTextMember && !string.IsNullOrEmpty(c.ComputedTextMemberAttribute.Format)).ToReadOnlyCollection();
			this.ComputedProperties = this.PersistedPropertiesWithoutBackreferences.Where(c => c.IsComputedMember && !string.IsNullOrEmpty(c.ComputedMemberAttribute.GetExpression)).ToReadOnlyCollection();
			
			this.PrimaryKeyDerivableProperties = this
				.ComputedProperties
				.Where(c => c.ComputedMemberAssignTarget != null)
				.ToList();
		
			if (this.PrimaryKeyProperties.Count(c => c.IsPropertyThatIsCreatedOnTheServerSide) > 1)
			{
				throw new InvalidDataAccessObjectModelDefinition("An object can only define one integer auto increment property");
			}

			this.IndexAttributes = new ReadOnlyCollection<IndexAttribute>(this.Type.GetCustomAttributes<IndexAttribute>(true).ToList());

			foreach (var attribute in this.IndexAttributes)
			{
				if (attribute.indexNameIfPropertyOrPropertyIfClass != null)
				{
					attribute.Properties = new [] { attribute.indexNameIfPropertyOrPropertyIfClass };
				}
			}

			var attributes = this.Type.GetCustomAttributes<OrganizationIndexAttribute>(true);

			if (attributes.Any())
			{
				if (attributes.Count() > 1)
				{
					throw new InvalidDataAccessModelDefinitionException($"Cannot define more than one {nameof(this.OrganizationIndexAttribute)} on type {this.TypeName}");
				}

				this.OrganizationIndexAttribute = attributes.Single();
			}

			if (this.PrimaryKeyProperties.Count(c => c.PropertyType.IsNullableType()) > 0)
			{
				throw new InvalidDataAccessObjectModelDefinition("The type {0} illegally defines a nullable primary key", this.Type.Name);
			}

			ValidateIndexes();
			ValidateOrganizationIndex();
		}

		private void ValidateIndexes()
		{
			if (this.IndexAttributes.Any(c => (c.Properties?.Length ?? 0) == 0))
			{
				throw new InvalidDataAccessModelDefinitionException($"The type {this.TypeName} contains class-defined indexes with no defined properties.");
			}

			var unknownProperties = this.IndexAttributes.SelectMany(c => c.Properties.Where(d => this.Type.GetProperty(d.Split(':')[0], BindingFlags.Public | BindingFlags.Instance)== null));
			
			if (unknownProperties.Any())
			{
				throw new InvalidDataAccessModelDefinitionException($"The type {this.TypeName} contains a class-defined index that references unknown properties '{string.Join(",", unknownProperties)}'");
			}

			foreach (var attribute in this.IndexAttributes.Where(c => c.Condition != null && c.Condition.Trim().Length > 0))
			{
				try
				{
					ComputedExpressionParser.Parse(attribute.Condition, Expression.Parameter(this.Type), null, null);
				}
				catch (Exception e)
				{
					throw new InvalidDataAccessModelDefinitionException($"The type {this.TypeName} contains a class-defined index with a condition that failed to parse: '{attribute.Condition}'");
				}
			}

			if (this.IndexAttributes.Any(c => c.Properties == null || c.Properties.Length == 0 || c.Properties.All(d => d.EndsWith(":IncludeOnly"))))
			{
				throw new InvalidDataAccessModelDefinitionException($"Property {this.TypeName} defines an index with no (non-includeonly) properties.");
			}
		}

		private void ValidateOrganizationIndex()
		{
			var properties = this.PersistedProperties
				.Where(c => c.OrganizationIndexAttribute != null)
				.ToList();

			if (this.OrganizationIndexAttribute != null)
			{
				if (properties.Count > 0)
				{
					throw new InvalidDataAccessModelDefinitionException($"The type {this.TypeName} should not have properties that define an {nameof(this.OrganizationIndexAttribute)} because an {nameof(this.OrganizationIndexAttribute)} is already defined at the class level");
				}

				if (this.OrganizationIndexAttribute.Disable)
				{
					if ((this.OrganizationIndexAttribute.Properties?.Length ?? 0) == 0)
					{
						throw new InvalidDataAccessModelDefinitionException($"The type {this.TypeName} defines a disabled {nameof(this.OrganizationIndexAttribute)} with a non null or empty Properties.");
					}
				}

				return;
			}

			if (properties.Count == 1)
			{
				if (properties[0].OrganizationIndexAttribute.Disable && !properties[0].IsPrimaryKey)
				{
					throw new InvalidDataAccessObjectModelDefinition($"Disabling an organization/clustered requires {nameof(this.OrganizationIndexAttribute)} to be applied to a primary key property but is instead applied to the property '{properties[0].PropertyName}'");
				}
			}
			else if (properties.Count > 1)
			{
				if (properties.Any(c => c.OrganizationIndexAttribute.Disable))
				{
					throw new InvalidDataAccessObjectModelDefinition($"You have defined and/or disabled the organization/clustered index on {this.TypeName} multiple times. Remove one or more of the [{nameof(this.OrganizationIndexAttribute)}] attributes.");
				}
			}
		}
	}
}
