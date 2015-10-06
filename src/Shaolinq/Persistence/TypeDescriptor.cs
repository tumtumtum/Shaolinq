// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using Platform;
using Platform.Collections;
using Platform.Reflection;

namespace Shaolinq.Persistence
{
	public class TypeDescriptor
	{
		public Type Type { get; }
		public TypeDescriptorProvider TypeDescriptorProvider { get; private set; }
		public IReadOnlyList<PropertyDescriptor> RelatedProperties { get; }
		public IReadOnlyList<PropertyDescriptor> PrimaryKeyProperties { get; }
		public IReadOnlyList<PropertyDescriptor> PersistedProperties { get; }
		public IReadOnlyList<PropertyDescriptor> PersistedAndRelatedObjectProperties { get; private set; }
		public IReadOnlyList<PropertyDescriptor> ComputedProperties { get; private set; }
		public IReadOnlyList<PropertyDescriptor> ComputedTextProperties { get; private set; }

		public string TypeName { get { return this.Type.Name; } }
		public bool HasPrimaryKeys { get { return this.PrimaryKeyProperties.Count > 0; } }
		public int PrimaryKeyCount { get { return this.PrimaryKeyProperties.Count; } }
		public DataAccessObjectAttribute DataAccessObjectAttribute { get; }

		private readonly IDictionary<TypeDescriptor, TypeRelationshipInfo> relationshipInfos;
		private readonly IDictionary<string, PropertyDescriptor> propertyDescriptorByColumnName;
		private readonly IDictionary<string, PropertyDescriptor> propertyDescriptorByPropertyName;
		private readonly Dictionary<Type, PropertyDescriptor> relatedPropertiesByType = new Dictionary<Type, PropertyDescriptor>();

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

		public string PersistedName
		{
			get
			{
				return this.DataAccessObjectAttribute.GetName(this.Type);
			}
		}

		public IEnumerable<TypeRelationshipInfo> GetRelationshipInfos()
		{
			return relationshipInfos.Values;
		}

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

		public PropertyDescriptor GetRelatedProperty(Type type)
		{
			PropertyDescriptor retval;

			if (!this.relatedPropertiesByType.TryGetValue(type, out retval))
			{
				Func<PropertyDescriptor, bool> isForType = delegate(PropertyDescriptor c)
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
				};

				retval = this.RelatedProperties.FirstOrDefault(isForType);

				if (retval == null)
				{
					throw new InvalidOperationException(String.Format("Unable to find related property for type '{0}' on type '{1}'", type.Name, this.Type.Name));
				}

				this.relatedPropertiesByType[type] = retval;
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

		public TypeDescriptor(TypeDescriptorProvider typeDescriptorProvider, Type type)
		{
			var propertyDescriptorsInOrder = new List<PropertyDescriptor>();
			
			this.Type = type;
			this.TypeDescriptorProvider = typeDescriptorProvider;

			this.DataAccessObjectAttribute = type.GetFirstCustomAttribute<DataAccessObjectAttribute>(true);
			
			this.relationshipInfos = new Dictionary<TypeDescriptor, TypeRelationshipInfo>();
			this.propertyDescriptorByColumnName = new Dictionary<string, PropertyDescriptor>();
			this.propertyDescriptorByPropertyName = new Dictionary<string, PropertyDescriptor>();
			
			var alreadyEnteredProperties = new HashSet<string>();

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
					var propertyDescriptor = new PropertyDescriptor(this, type, propertyInfo);

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
						throw new InvalidDataAccessObjectModelDefinition("The property {0} on {1} is not virtual or abstract", propertyInfo.Name, type.Name);
					}

					propertyDescriptorsInOrder.Add(propertyDescriptor);
					propertyDescriptorByPropertyName[propertyInfo.Name] = propertyDescriptor;
					propertyDescriptorByColumnName[attribute.GetName(propertyInfo, this)] = propertyDescriptor;
				}
			}

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
						throw new InvalidDataAccessObjectModelDefinition("The property {0} on {1} is not virtual or abstract", propertyInfo.Name, type.Name);
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
						throw new InvalidDataAccessObjectModelDefinition("The property {0} on {1} is not virtual or abstract", propertyInfo.Name, type.Name);
					}

					var propertyDescriptor = new PropertyDescriptor(this, this.Type, propertyInfo);

					relatedProperties.Add(propertyDescriptor);

					propertyDescriptorByPropertyName[propertyInfo.Name] = propertyDescriptor;
				}
			}

			this.RelatedProperties = new ReadOnlyList<PropertyDescriptor>(relatedProperties);
			this.PersistedProperties = new ReadOnlyList<PropertyDescriptor>(propertyDescriptorsInOrder);
			this.PrimaryKeyProperties = new ReadOnlyList<PropertyDescriptor>(this.PersistedProperties.Where(propertyDescriptor => propertyDescriptor.IsPrimaryKey).ToList());
			this.ComputedTextProperties = new ReadOnlyList<PropertyDescriptor>(this.PersistedProperties.Where(c => c.IsComputedTextMember && !String.IsNullOrEmpty(c.ComputedTextMemberAttribute.Format)).ToList());
			this.ComputedProperties = new ReadOnlyList<PropertyDescriptor>(this.PersistedProperties.Where(c => c.IsComputedMember && !String.IsNullOrEmpty(c.ComputedMemberAttribute.Expression)).ToList());
			this.PersistedAndRelatedObjectProperties = new ReadOnlyList<PropertyDescriptor>(this.PersistedProperties.Concat(this.RelatedProperties.Where(c => c.IsBackReferenceProperty)).ToList());

			if (this.PrimaryKeyProperties.Count(c => c.IsPropertyThatIsCreatedOnTheServerSide) > 1)
			{
				throw new InvalidDataAccessObjectModelDefinition("An object can only define one integer auto increment property");
			}
		}

		public override string ToString()
		{
			return "TypeDescriptor: " + this.Type.Name;
		}
	}
}
