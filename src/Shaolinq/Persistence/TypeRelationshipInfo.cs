// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)
namespace Shaolinq.Persistence
{
	public class TypeRelationshipInfo
	{
		public TypeDescriptor RelatedTypeTypeDescriptor { get; set; }
		public PropertyDescriptor ReferencingProperty { get; internal set; }
		public RelationshipType RelationshipType { get; internal set; }
		
		public TypeRelationshipInfo(TypeDescriptor relatedTypeDescriptor, RelationshipType relationshipType, PropertyDescriptor relatedProperty)
		{
			this.RelatedTypeTypeDescriptor = relatedTypeDescriptor;
			this.RelationshipType = relationshipType;
			this.ReferencingProperty = relatedProperty;
		}
	}
}
