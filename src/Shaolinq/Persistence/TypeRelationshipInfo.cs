// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)

using System.Collections.Generic;

namespace Shaolinq.Persistence
{
	public class TypeRelationshipInfo
	{
		public RelationshipType RelationshipType { get; set; }
		public PropertyDescriptor ReferencingProperty { get; set; }
		public PropertyDescriptor TargetProperty { get; set; }
		public TypeDescriptor TargetType => this.TargetProperty.DeclaringTypeDescriptor;
		public TypeDescriptor ReferencingType => this.ReferencingProperty.DeclaringTypeDescriptor;

		public TypeRelationshipInfo(RelationshipType relationshipType, PropertyDescriptor referencingProperty, PropertyDescriptor targetProperty)
		{
			this.RelationshipType = relationshipType;
			this.ReferencingProperty = referencingProperty;
			this.TargetProperty = targetProperty;
		}
	}

	public class TypeRelationshipInfoEqualityComparer
		: IEqualityComparer<TypeRelationshipInfo>
	{
		public static readonly TypeRelationshipInfoEqualityComparer Default = new TypeRelationshipInfoEqualityComparer();

		public bool Equals(TypeRelationshipInfo x, TypeRelationshipInfo y)
		{
			return x.RelationshipType == y.RelationshipType
				   && x.TargetProperty == y.TargetProperty
				   && x.ReferencingProperty == y.ReferencingProperty;
		}

		public int GetHashCode(TypeRelationshipInfo obj)
		{
			return obj.RelationshipType.GetHashCode() ^ obj.ReferencingProperty.GetHashCode() ^ obj.TargetProperty.GetHashCode();
		}
	}
}
