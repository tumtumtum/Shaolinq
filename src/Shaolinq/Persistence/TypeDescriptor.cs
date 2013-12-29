// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using Platform;
using Platform.Reflection;

namespace Shaolinq.Persistence
{
	public class TypeDescriptor
	{
		public Type Type
		{
			get;
			private set;
		}

		public bool HasPrimaryKeys
		{
			get;
			private set;
		}

		public int PrimaryKeyCount
		{
			get;
			private set;
		}

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

		public string GetPersistedName(DataAccessModel model)
		{
			var connection = model.GetDatabaseConnection(this.Type);
		
			return GetPersistedName(connection);
		}

		public string GetPersistedName(SqlDatabaseContext databaseConnection)
		{
			return (databaseConnection.SchemaNamePrefix ?? "") + this.DataAccessObjectAttribute.GetName(this.Type);
		}

		public DataAccessObjectAttribute DataAccessObjectAttribute { get; private set; }

		public IEnumerable<TypeRelationshipInfo> GetRelationshipInfos()
		{
			return relationshipInfos.Values;
		}

		private readonly IDictionary<TypeDescriptor, TypeRelationshipInfo> relationshipInfos; 
		private readonly IDictionary<string, PropertyDescriptor> propertyDescriptorByColumnName;
		private readonly IDictionary<string, PropertyDescriptor> propertyDescriptorByPropertyName;
		private readonly IDictionary<PropertyInfo, PropertyDescriptor> propertyDescriptorsByPropertyInfo;
		
		public TypeRelationshipInfo GetRelationshipInfo(TypeDescriptor relatedTypeDescriptor)
		{
			TypeRelationshipInfo retval;

			if (relationshipInfos.TryGetValue(relatedTypeDescriptor, out retval))
			{
				return retval;
			}

			return null;
		}

		public TypeRelationshipInfo SetOrCreateRelationshipInfo(TypeDescriptor relatedTypeDescriptor, EntityRelationshipType entityRelationshipType, PropertyDescriptor relatedProperty)
		{
			TypeRelationshipInfo retval;

			if (relationshipInfos.TryGetValue(relatedTypeDescriptor, out retval))
			{
				retval.EntityRelationshipType = entityRelationshipType;
				retval.ReferencingProperty = relatedProperty;
				retval.RelatedTypeTypeDescriptor = relatedTypeDescriptor;

				return retval;
			}

			retval = new TypeRelationshipInfo(relatedTypeDescriptor, entityRelationshipType, relatedProperty);

			relationshipInfos[relatedTypeDescriptor] = retval;

			return retval;
		}
        
		public ICollection<PropertyDescriptor> PrimaryKeyProperties
		{
			get;
			private set;
		}

		public PropertyDescriptor GetPropertyDescriptorByColumnName(string columnName)
		{
			PropertyDescriptor retval;

			if (!propertyDescriptorByColumnName.TryGetValue(columnName, out retval))
			{
				return null;
			}

			return retval;
		}

		public PropertyDescriptor GetPropertyDescriptorByPropertyName(string propertyName)
		{
			PropertyDescriptor retval;

			if (!propertyDescriptorByPropertyName.TryGetValue(propertyName, out retval))
			{
				return null;
			}

			return retval;
		}

		public ICollection<PropertyDescriptor> RelatedProperties
		{
			get;
			private set;
		}

		public PropertyDescriptor GetRelatedProperty(Type type)
		{
			PropertyDescriptor retval;

			if (!relatedProperties.TryGetValue(type, out retval))
			{
				retval = this.RelatedProperties.Filter
					(
					c =>
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
					).FirstOrDefault();

				if (retval == null)
				{
					throw new InvalidOperationException(String.Format("Unable to find related property for type '{0}' on type '{1}'", type.Name, this.Type.Name));
				}

				relatedProperties[type] = retval;
			}

			return retval;
		}

		private readonly Dictionary<Type, PropertyDescriptor> relatedProperties = new Dictionary<Type, PropertyDescriptor>();

		public ICollection<PropertyDescriptor> PersistedProperties
		{
			get
			{
				if (persistedProperties == null)
				{
					persistedProperties = new ReadOnlyCollection<PropertyDescriptor>(propertyDescriptorsByPropertyInfo.Values.ToList());
				}

				return persistedProperties;
			}
		}
		private ICollection<PropertyDescriptor> persistedProperties;


		public ICollection<PropertyDescriptor> ReferencedObjectPrimaryKeyProperties
		{
			get;
			private set;
		}

		public ICollection<PropertyDescriptor> ComputedTextProperties
		{
			get;
			private set;
		}

		private bool IsValidDataType(Type type)
		{
			var underlyingType = Nullable.GetUnderlyingType(type);

			if (underlyingType != null)
			{
				return IsValidDataType(underlyingType);
			}

			if (type.IsGenericType)
			{
				return type.GetGenericTypeDefinition() == typeof(IList<>)
				       || type.GetGenericTypeDefinition() == typeof(IDictionary<,>);
			}

			return type.IsPrimitive
				|| type.IsEnum
				|| type == typeof(Decimal)
				|| type == typeof(DateTime)
			    || type.IsDataAccessObjectType()
			    || type == typeof(Guid)
			    || type == typeof(TimeSpan)
			    || type == typeof(string)
			    || type == typeof(byte[]);
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

		public TypeDescriptor(Type type)
		{
			this.Type = type;

			this.DataAccessObjectAttribute = type.GetFirstCustomAttribute<DataAccessObjectAttribute>(true);
			
			relationshipInfos = new Dictionary<TypeDescriptor, TypeRelationshipInfo>();
			propertyDescriptorByColumnName = new Dictionary<string, PropertyDescriptor>();
			propertyDescriptorByPropertyName = new Dictionary<string, PropertyDescriptor>();
			propertyDescriptorsByPropertyInfo = new Dictionary<PropertyInfo, PropertyDescriptor>();
			
			var alreadyEnteredProperties = new HashSet<string>();

			var referencedObjectPrimaryKeyProperties = new List<PropertyDescriptor>();

			this.ReferencedObjectPrimaryKeyProperties = referencedObjectPrimaryKeyProperties;

			foreach (var propertyInfo in this.GetPropertiesInOrder())
			{
				if (alreadyEnteredProperties.Contains(propertyInfo.Name))
				{
					continue;
				}

				alreadyEnteredProperties.Add(propertyInfo.Name);

				var attribute = propertyInfo.GetFirstCustomAttribute<PersistedMemberAttribute>(true);

				if (attribute != null)
				{
					PropertyDescriptor propertyDescriptor;

					propertyDescriptor = new PropertyDescriptor(this, type, propertyInfo);

					if (propertyInfo.GetGetMethod() == null)
					{
						throw new InvalidDataAccessObjectModelDefinition("The property {0} is missing a required getter method", propertyInfo.Name);
					}

					if (propertyInfo.GetSetMethod() == null && !propertyDescriptor.IsComputedTextMember)
					{
						throw new InvalidDataAccessObjectModelDefinition("The property {0} is missing a required setter method", propertyInfo.Name);
					}

					if (!IsValidDataType(propertyInfo.PropertyType))
					{
						throw new InvalidDataAccessObjectModelDefinition("The property {0} cannot have a return type of {1}", propertyInfo.Name, propertyInfo.PropertyType.Name);
					}
					
					if (!(propertyInfo.GetGetMethod().IsAbstract || propertyInfo.GetGetMethod().IsVirtual))
					{
						throw new InvalidDataAccessObjectModelDefinition("The property {0} is not virtual or abstract", propertyInfo.Name);
					}

					
					propertyDescriptorsByPropertyInfo[propertyInfo] = propertyDescriptor;
					propertyDescriptorByPropertyName[propertyInfo.Name] = propertyDescriptor;
					propertyDescriptorByColumnName[attribute.GetName(propertyInfo)] = propertyDescriptor;
				}

				var referencedPrimaryKeyAttribute = propertyInfo.GetFirstCustomAttribute<ReferencedObjectPrimaryKeyPropertyAttribute>(true);

				if (referencedPrimaryKeyAttribute != null)
				{
					PropertyInfo referencedObjectPrimaryKeyPropertyInfo = null;

					if (propertyInfo.GetSetMethod() != null)
					{
						throw new InvalidDataAccessObjectModelDefinition("The ReferencedObjectPrimaryKeyProperty '{0}' illegally defines a setter method", propertyInfo.Name);
					}

					if (propertyInfo.GetGetMethod() == null)
					{
						throw new InvalidDataAccessObjectModelDefinition("The ReferencedObjectPrimaryKeyProperty '{0}' must have a setter method", propertyInfo.Name);
					}

					var ownerProperty = propertyDescriptorsByPropertyInfo.Values.FirstOrDefault
					(
						c =>
						{
							if (!propertyInfo.Name.StartsWith(c.PropertyName))
							{
								return false;
							}

							if (!c.PropertyType.IsDataAccessObjectType())
							{
								return false;
							}

							string referencedPropertyName = propertyInfo.Name.Substring(c.PropertyName.Length);

							if ((referencedObjectPrimaryKeyPropertyInfo  = c.PropertyType.GetProperties().FirstOrDefault(d => d.Name == referencedPropertyName)) == null)
							{
								return false;
							}

							return true;
						}
					);

					if (ownerProperty != null)
					{
						var propertyDescriptor = new PropertyDescriptor(this, type, propertyInfo);
						propertyDescriptor.referencedObjectPrimaryKeyPropertyInfo = referencedObjectPrimaryKeyPropertyInfo;
						referencedObjectPrimaryKeyProperties.Add(propertyDescriptor);
					}
				}
			}

			// Load related properties

			var relatedProperties = new List<PropertyDescriptor>();

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
						throw new InvalidDataAccessObjectModelDefinition("The property {0} on {1} is decorated with a RelatedDataAccessObjectsAttribute but the property type does not extend RelatedDataAccessObjects<OBJECT_TYPE>", this.Type.Name, propertyInfo.Name);
					}

					if (propertyInfo.GetSetMethod() != null)
					{
						throw new InvalidDataAccessObjectModelDefinition("The property {0} is a related objects property and should not define a setter method", propertyInfo.Name);
					}

					if (propertyInfo.GetGetMethod() == null)
					{
						throw new InvalidDataAccessObjectModelDefinition("The property {0} is missing a required getter method", propertyInfo.Name);
					}

					if (!(propertyInfo.GetGetMethod().IsAbstract || propertyInfo.GetGetMethod().IsVirtual))
					{
						throw new InvalidDataAccessObjectModelDefinition("The property {0} is not virtual or abstract", propertyInfo.Name);
					}

					var propertyDescriptor = new PropertyDescriptor(this, this.Type, propertyInfo);

					relatedProperties.Add(propertyDescriptor);

					propertyDescriptorByPropertyName[propertyInfo.Name] = propertyDescriptor;
				}
				else if (propertyInfo.GetFirstCustomAttribute<BackReferenceAttribute>(true) != null)
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
						throw new InvalidDataAccessObjectModelDefinition("The property {0} is not virtual or abstract", propertyInfo.Name);
					}

					var propertyDescriptor = new PropertyDescriptor(this, this.Type, propertyInfo);

					relatedProperties.Add(propertyDescriptor);

					propertyDescriptorByPropertyName[propertyInfo.Name] = propertyDescriptor;
				}
			}

			this.RelatedProperties = new ReadOnlyCollection<PropertyDescriptor>(relatedProperties);
            
			var primaryKeys = new List<PropertyDescriptor>();

			foreach (var propertyDescriptor in this.PersistedProperties)
			{
				var primaryKeyAttribute = propertyDescriptor.PropertyInfo.GetFirstCustomAttribute<PrimaryKeyAttribute>(true);

				if (primaryKeyAttribute != null && primaryKeyAttribute.IsPrimaryKey)
				{
					primaryKeys.Add(propertyDescriptor);
				}
			}

			this.PrimaryKeyProperties = new ReadOnlyCollection<PropertyDescriptor>(primaryKeys);

			this.HasPrimaryKeys = primaryKeys.Count > 0;
			this.PrimaryKeyCount = primaryKeys.Count;

			var computedTextProperties = this.persistedProperties.Where(c => c.ComputedTextMemberAttribute != null && !String.IsNullOrEmpty(c.ComputedTextMemberAttribute.Format)).ToList();
			this.ComputedTextProperties = new ReadOnlyCollection<PropertyDescriptor>(computedTextProperties);
		}

		public override string ToString()
		{
			return "TypeDescriptor: "+ this.Type.ToString();
		}
	}
}
