// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)
namespace Shaolinq.Persistence
{
	public class TypeRelationshipInfo
	{
		public RelationshipType RelationshipType { get; set; }
		public PropertyDescriptor ReferencingProperty { get; set; }
		public PropertyDescriptor TargetProperty { get; set; }
		public TypeDescriptor TargetType => TargetProperty.DeclaringTypeDescriptor;
		public TypeDescriptor ReferencingType => ReferencingProperty.DeclaringTypeDescriptor;

		public TypeRelationshipInfo(RelationshipType relationshipType, PropertyDescriptor referencingProperty, PropertyDescriptor targetProperty)
		{
			this.RelationshipType = relationshipType;
			this.ReferencingProperty = referencingProperty;
			this.TargetProperty = targetProperty;
		}
	}
}
