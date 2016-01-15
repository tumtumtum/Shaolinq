// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Platform;
using Platform.Reflection;

namespace Shaolinq.Persistence
{
	public class TypeDescriptor
	{
		public Type Type { get; }
		public string PersistedName { get; }
		public TypeDescriptorProvider TypeDescriptorProvider { get; }
		public DataAccessObjectAttribute DataAccessObjectAttribute { get; }
		public IReadOnlyList<PropertyDescriptor> RelationshipRelatedProperties { get; }
		public IReadOnlyList<PropertyDescriptor> PrimaryKeyProperties { get; }
		public IReadOnlyList<PropertyDescriptor> PersistedProperties { get; }
		public IReadOnlyList<PropertyDescriptor> PersistedAndBackReferenceProperties { get; }
		public IReadOnlyList<PropertyDescriptor> ComputedProperties { get; }
		public IReadOnlyList<PropertyDescriptor> ComputedTextProperties { get; }

		public string TypeName => this.Type.Name;
		public bool HasPrimaryKeys => this.PrimaryKeyProperties.Count > 0;
		public int PrimaryKeyCount => this.PrimaryKeyProperties.Count;
		
		private readonly List<TypeRelationshipInfo> relationshipInfos;
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
		
		public Expression GetPrimaryKeyExpression(Expression obj)
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
			PropertyDescriptor retval;

			if (!this.propertyDescriptorByColumnName.TryGetValue(columnName, out retval))
			{
				return null;
			}

			return retval;
		}

		public PropertyDescriptor GetPropertyDescriptorByPropertyName(string propertyName)
		{
			PropertyDescriptor retval;

			if (!this.propertyDescriptorByPropertyName.TryGetValue(propertyName, out retval))
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

				retval = this.RelationshipRelatedProperties.FirstOrDefault(isForType);

				if (retval == null)
				{
					throw new InvalidOperationException($"Unable to find related property for type '{type.Name}' on type '{this.Type.Name}'");
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

		internal void AddRelationshipInfo(RelationshipType relationshipType, PropertyDescriptor relatingProperty, PropertyDescriptor targetProperty)
		{
			this.relationshipInfos.Add(new TypeRelationshipInfo(relationshipType, relatingProperty, targetProperty));
		}

		public TypeDescriptor(TypeDescriptorProvider typeDescriptorProvider, Type type)
		{
			var propertyDescriptorsInOrder = new List<PropertyDescriptor>();
			
			this.Type = type;
			this.TypeDescriptorProvider = typeDescriptorProvider;

			this.DataAccessObjectAttribute = type.GetFirstCustomAttribute<DataAccessObjectAttribute>(true);

			this.relationshipInfos = new List<TypeRelationshipInfo>();
			this.propertyDescriptorByColumnName = new Dictionary<string, PropertyDescriptor>();
			this.propertyDescriptorByPropertyName = new Dictionary<string, PropertyDescriptor>();
			
			var alreadyEnteredProperties = new HashSet<string>();

			this.PersistedName = this.DataAccessObjectAttribute.GetName(this, this.TypeDescriptorProvider.Configuration.NamingTransforms?.DataAccessObjectName);

			foreach (var propertyInfo in this.GetPropertiesInOrder())
			{
				if (alreadyEnteredProperties.Contains(propertyInfo.Name))
				{
					continue;
				}

				alreadyEnteredProperties.Add(propertyInfo.Name);

				var attribute = propertyInfo.GetFirstCustomAttribute<PersistedMemberAttribute>(true);

				if (attribute != null && attribute.GetType() == typeof(PersistedMemberAttribute))
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
					this.propertyDescriptorByPropertyName[propertyInfo.Name] = propertyDescriptor;
					this.propertyDescriptorByColumnName[attribute.GetName(propertyDescriptor)] = propertyDescriptor;
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

					this.propertyDescriptorByPropertyName[propertyInfo.Name] = propertyDescriptor;
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

					this.propertyDescriptorByPropertyName[propertyInfo.Name] = propertyDescriptor;
				}
			}

			this.RelationshipRelatedProperties = relatedProperties.ToReadOnlyCollection();
			this.PersistedProperties = propertyDescriptorsInOrder.ToReadOnlyCollection();
			this.PrimaryKeyProperties = this.PersistedProperties.Where(propertyDescriptor => propertyDescriptor.IsPrimaryKey).ToReadOnlyCollection();
			this.ComputedTextProperties = this.PersistedProperties.Where(c => c.IsComputedTextMember && !String.IsNullOrEmpty(c.ComputedTextMemberAttribute.Format)).ToReadOnlyCollection();
			this.ComputedProperties = this.PersistedProperties.Where(c => c.IsComputedMember && !String.IsNullOrEmpty(c.ComputedMemberAttribute.GetExpression)).ToReadOnlyCollection();
			this.PersistedAndBackReferenceProperties = this.PersistedProperties.Concat(this.RelationshipRelatedProperties.Where(c => c.IsBackReferenceProperty)).ToReadOnlyCollection();

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
