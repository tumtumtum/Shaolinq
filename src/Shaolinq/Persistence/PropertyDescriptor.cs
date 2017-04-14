// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Platform;
using Platform.Reflection;
using Platform.Validation;
using Shaolinq.Persistence.Linq;
using DefaultValueAttribute = Platform.Validation.DefaultValueAttribute;

namespace Shaolinq.Persistence
{
	public class PropertyDescriptor
	{
		public Type OwnerType { get; }
		public bool IsPrimaryKey { get; }
		public string PersistedName { get; }
		public string SuffixName { get; }
		public string PrefixName { get; }
		public PropertyInfo PropertyInfo { get; }
		public UniqueAttribute UniqueAttribute { get; }
		public TypeDescriptor DeclaringTypeDescriptor { get; }
		public PropertyInfo ComputedMemberAssignTarget { get; }
		public Expression ComputedMemberAssignmentValue { get; }
		public TypeDescriptor PropertyTypeTypeDescriptor => this.DeclaringTypeDescriptor.TypeDescriptorProvider.GetTypeDescriptor(this.PropertyType);
		public PrimaryKeyAttribute PrimaryKeyAttribute { get; }
		public OrganizationKeyAttribute OrganizationKeyAttribute { get; }
		public DefaultValueAttribute DefaultValueAttribute { get; }
		public AutoIncrementAttribute AutoIncrementAttribute { get; }
		public ValueRequiredAttribute ValueRequiredAttribute { get; }
		public BackReferenceAttribute BackReferenceAttribute { get; }
		public PersistedMemberAttribute PersistedMemberAttribute { get; }
		public ForeignObjectConstraintAttribute ForeignObjectConstraintAttribute { get; }
		public IReadOnlyList<IndexAttribute> IndexAttributes { get; }
		public ComputedMemberAttribute ComputedMemberAttribute { get; }
		public ComputedTextMemberAttribute ComputedTextMemberAttribute { get; }
		public RelatedDataAccessObjectsAttribute RelatedDataAccessObjectsAttribute { get; }
		public object DefaultValue { get; }
		public bool HasDefaultValue { get; }
		public string PropertyName => this.PropertyInfo.Name;
		public Type PropertyType => this.PropertyInfo?.PropertyType;
		public bool HasUniqueAttribute => this.UniqueAttribute != null;
		public bool IsBackReferenceProperty => this.BackReferenceAttribute != null;
		public bool IsComputedMember => this.ComputedMemberAttribute != null;
		public bool IsComputedTextMember => this.ComputedTextMemberAttribute != null;
		public bool IsRelatedDataAccessObjectsProperty => this.RelatedDataAccessObjectsAttribute != null;
		public bool IsAutoIncrement => this.AutoIncrementAttribute != null && this.AutoIncrementAttribute.AutoIncrement;
		public bool IsPropertyThatIsCreatedOnTheServerSide => this.IsAutoIncrement && this.PropertyType.IsIntegerType(true);

		public static bool IsPropertyPrimaryKey(PropertyInfo propertyInfo)
		{
			var value =  propertyInfo?.GetFirstCustomAttribute<PrimaryKeyAttribute>(true);

			return value != null && value.IsPrimaryKey;
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
			this.ComputedMemberAttribute = propertyInfo.GetFirstCustomAttribute<ComputedMemberAttribute>(true);
			this.ComputedTextMemberAttribute = propertyInfo.GetFirstCustomAttribute<ComputedTextMemberAttribute>(true);
			this.ForeignObjectConstraintAttribute = propertyInfo.GetFirstCustomAttribute<ForeignObjectConstraintAttribute>(true);

			if (this.PersistedMemberAttribute == null)
			{
				this.PersistedMemberAttribute = (PersistedMemberAttribute)this.ComputedMemberAttribute ?? this.ComputedTextMemberAttribute;
			}

			if (this.PropertyType.IsIntegerType(true) || this.PropertyType.GetUnwrappedNullableType() == typeof(Guid))
			{
				this.AutoIncrementAttribute = propertyInfo.GetFirstCustomAttribute<AutoIncrementAttribute>(true);
			}

			this.PrimaryKeyAttribute = this.PropertyInfo.GetFirstCustomAttribute<PrimaryKeyAttribute>(true);
			this.OrganizationKeyAttribute = this.PropertyInfo.GetFirstCustomAttribute<OrganizationKeyAttribute>(true);
			this.IsPrimaryKey = this.PrimaryKeyAttribute != null && this.PrimaryKeyAttribute.IsPrimaryKey;
			this.IndexAttributes = this.PropertyInfo.GetCustomAttributes(typeof(IndexAttribute), true).OfType<IndexAttribute>().ToReadOnlyCollection();
			this.UniqueAttribute = this.PropertyInfo.GetFirstCustomAttribute<UniqueAttribute>(true);

			var named = this.PersistedMemberAttribute ?? this.BackReferenceAttribute ?? (NamedMemberAttribute)this.RelatedDataAccessObjectsAttribute;

			if (named != null)
			{
				this.PersistedName = named.GetName(this, this.DeclaringTypeDescriptor.TypeDescriptorProvider.Configuration.NamingTransforms?.PersistedMemberName);
				this.PrefixName = named.GetPrefixName(this, this.DeclaringTypeDescriptor.TypeDescriptorProvider.Configuration.NamingTransforms?.PersistedMemberPrefixName);
				this.SuffixName = named.GetSuffixName(this, this.DeclaringTypeDescriptor.TypeDescriptorProvider.Configuration.NamingTransforms?.PersistedMemberSuffixName);
			}

			var expression = this.ComputedMemberAttribute?.GetSetLambdaExpression(this.DeclaringTypeDescriptor.TypeDescriptorProvider.Configuration, this)?.Body.StripConvert();

			if (expression?.NodeType == ExpressionType.Assign)
			{
				var assignmentExpression = expression as BinaryExpression;

				if (assignmentExpression.Left.NodeType == ExpressionType.MemberAccess)
				{
					var memberAccess = assignmentExpression.Left as MemberExpression;

					this.ComputedMemberAssignTarget = memberAccess.Member as PropertyInfo;
					this.ComputedMemberAssignmentValue = assignmentExpression.Right;
				}
			}
			
			var implicitDefault = this.PropertyType.IsValueType && this.DeclaringTypeDescriptor.TypeDescriptorProvider.Configuration.ValueTypesAutoImplicitDefault;

			if (this.DefaultValueAttribute != null || (implicitDefault && !(this.IsAutoIncrement && this.PropertyType.IsIntegerType())))
			{
				this.DefaultValue = this.DefaultValueAttribute?.Value;
				this.HasDefaultValue = true;

				if (implicitDefault)
				{
					this.DefaultValue = this.PropertyType.GetDefaultValue();
				}

				try
				{
					try
					{
						this.DefaultValue = Convert.ChangeType(this.DefaultValue, this.PropertyType);
					}
					catch (InvalidCastException)
					{
						var converter = System.ComponentModel.TypeDescriptor.GetConverter(this.PropertyType);

						this.DefaultValue = converter.ConvertFrom(this.DefaultValue);
					}
				}
				catch (InvalidCastException)
				{
					throw new InvalidDataAccessObjectModelDefinition($"The property '{propertyInfo.DeclaringType.Name}.{this.PropertyName}' has an incompatible default value");
				}
			}
		}

		public static implicit operator PropertyInfo(PropertyDescriptor value)
		{
			return value.PropertyInfo;
		}

		public override bool Equals(object obj)
		{
			var value = obj as PropertyDescriptor;

			return value != null && value.PropertyInfo == this.PropertyInfo;
		}

		public override int GetHashCode() => this.PropertyInfo.GetHashCode();
		public override string ToString() => $"{this.DeclaringTypeDescriptor.TypeName}.{this.PropertyName} [PropertyDescriptor]";
		public TypeRelationshipInfo RelationshipInfo => this.DeclaringTypeDescriptor.GetRelationshipInfos().SingleOrDefault(c => c.ReferencingProperty == this);
	}
}
