// Copyright (c) 2007-2014 Thong Nguyen (tumtumtum@gmail.com)

﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using Platform;
using Platform.Reflection;
using Platform.Validation;

namespace Shaolinq.Persistence
{
	public class PropertyDescriptor
	{
		public TypeDescriptorProvider TypeDescriptorProvider
		{
			get
			{
				return this.DeclaringTypeDescriptor.TypeDescriptorProvider;
			}
		}

		public Type OwnerType { get; private set; }
		public bool IsPrimaryKey { get; private set; }
		public string PersistedName { get; private set; }
		public string PersistedShortName { get; private set; }
		public PropertyInfo PropertyInfo { get; private set; }
		public UniqueAttribute UniqueAttribute { get; private set; }
		public TypeDescriptor DeclaringTypeDescriptor { get; private set; }
		public PrimaryKeyAttribute PrimaryKeyAttribute { get; private set; }
		public DefaultValueAttribute DefaultValueAttribute { get; private set; }
		public AutoIncrementAttribute AutoIncrementAttribute { get; private set; }
		public ValueRequiredAttribute ValueRequiredAttribute { get; private set; }
		public BackReferenceAttribute BackReferenceAttribute { get; private set; }
		public PersistedMemberAttribute PersistedMemberAttribute { get; private set; }
		public ReadOnlyCollection<IndexAttribute> IndexAttributes { get; private set; }
		public ComputedTextMemberAttribute ComputedTextMemberAttribute { get; private set; }
		public RelatedDataAccessObjectsAttribute RelatedDataAccessObjectsAttribute { get; private set; }
		public ReferencedObjectPrimaryKeyPropertyAttribute ReferencedObjectPrimaryKeyPropertyAttribute { get; private set; }

		public string PropertyName
		{
			get
			{
				return this.PropertyInfo.Name;
			}
		}

		public bool HasUniqueAttribute
		{
			get
			{
				return this.UniqueAttribute != null;
			}
		}

		public PropertyDescriptor ReferencedObjectPrimaryKeyPropertyDescriptor
		{
			get
			{
				var typeDescriptor = this.TypeDescriptorProvider.GetTypeDescriptor(referencedObjectPrimaryKeyPropertyInfo.ReflectedType);

				return typeDescriptor.GetPropertyDescriptorByPropertyName(referencedObjectPrimaryKeyPropertyInfo.Name);
			}
		}
		internal PropertyInfo referencedObjectPrimaryKeyPropertyInfo;
		
		public bool IsReferencedObjectPrimaryKeyProperty
		{
			get
			{
				return this.ReferencedObjectPrimaryKeyPropertyAttribute != null;
			}
		}

		public bool IsRelatedDataAccessObjectsProperty
		{
			get
			{
				return this.RelatedDataAccessObjectsAttribute != null;
			}
		}

		public bool IsBackReferenceProperty
		{
			get
			{
				return this.BackReferenceAttribute != null;
			}
		}
        
		public bool IsAutoIncrement
		{
			get
			{
				return this.AutoIncrementAttribute != null
					&& this.AutoIncrementAttribute.AutoIncrement;
			}
		}

		public bool IsPropertyThatIsCreatedOnTheServerSide
		{
			get
			{
				return this.IsAutoIncrement && this.PropertyType.IsIntegerType();
			}
		}

		public Type PropertyType
		{
			get
			{
				return this.PropertyInfo.PropertyType;
			}
		}

		public bool IsComputedTextMember
		{
			get
			{
				return this.ComputedTextMemberAttribute != null;
			}
		}

		public PropertyDescriptor(TypeDescriptor declaringTypeDescriptor, Type ownerType, PropertyInfo propertyInfo)
		{
			this.OwnerType = ownerType;
			this.PropertyInfo = propertyInfo;
			this.DeclaringTypeDescriptor = declaringTypeDescriptor;

			this.ValueRequiredAttribute = propertyInfo.GetFirstCustomAttribute<ValueRequiredAttribute>(true);
			this.DefaultValueAttribute = propertyInfo.GetFirstCustomAttribute<DefaultValueAttribute>(true);
			this.BackReferenceAttribute = propertyInfo.GetFirstCustomAttribute<BackReferenceAttribute>(true);
			this.RelatedDataAccessObjectsAttribute = propertyInfo.GetFirstCustomAttribute<RelatedDataAccessObjectsAttribute>(true);
			this.PersistedMemberAttribute = propertyInfo.GetFirstCustomAttribute<PersistedMemberAttribute>(true);
			this.ReferencedObjectPrimaryKeyPropertyAttribute = propertyInfo.GetFirstCustomAttribute<ReferencedObjectPrimaryKeyPropertyAttribute>(true);
			this.ComputedTextMemberAttribute = propertyInfo.GetFirstCustomAttribute<ComputedTextMemberAttribute>(true);

			if (this.PropertyType.IsIntegerType() || this.PropertyType == typeof(Guid))
			{
				this.AutoIncrementAttribute = propertyInfo.GetFirstCustomAttribute<AutoIncrementAttribute>(true);
			}

			this.PrimaryKeyAttribute = this.PropertyInfo.GetFirstCustomAttribute<PrimaryKeyAttribute>(true);
			this.IsPrimaryKey = this.PrimaryKeyAttribute != null && this.PrimaryKeyAttribute.IsPrimaryKey;

			if (this.PersistedMemberAttribute != null)
			{
				this.PersistedName = this.PersistedMemberAttribute.GetName(this.PropertyInfo, declaringTypeDescriptor);
				this.PersistedShortName = this.PersistedMemberAttribute.GetShortName(this.PropertyInfo, this.DeclaringTypeDescriptor);
			}
			else if (this.BackReferenceAttribute != null)
			{
				this.PersistedName = this.BackReferenceAttribute.GetName(this.PropertyInfo, declaringTypeDescriptor);
				this.PersistedShortName = this.PersistedName;
			}
			else if (this.RelatedDataAccessObjectsAttribute != null)
			{
				this.PersistedName = propertyInfo.Name;
				this.PersistedShortName = propertyInfo.Name;
			}

			var indexAttributes = new List<IndexAttribute>();

			foreach (IndexAttribute indexAttribute in this.PropertyInfo.GetCustomAttributes(typeof(IndexAttribute), true))
			{
				if (indexAttribute.IndexName == null)
				{
					var clone = (IndexAttribute)indexAttribute.Clone();

					if (clone.LowercaseIndex)
					{
						clone.IndexName = this.PersistedName + "ToLower";
					}
					else
					{
						clone.IndexName = this.PersistedName;
					}

					indexAttributes.Add(clone);
				}
				else
				{
					indexAttributes.Add(indexAttribute);
				}
			}

			this.IndexAttributes = new ReadOnlyCollection<IndexAttribute>(indexAttributes);
			this.UniqueAttribute = this.PropertyInfo.GetFirstCustomAttribute<UniqueAttribute>(true);
		}

		public override string ToString()
		{
			return string.Format("PropertyDescriptor: {0}.{1}", this.DeclaringTypeDescriptor.TypeName, this.PropertyName);
		}
	}
}
