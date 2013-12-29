// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

﻿namespace Shaolinq.Persistence
{
	public class TypeRelationshipInfo
	{
		public TypeDescriptor RelatedTypeTypeDescriptor { get; set; }
		public PropertyDescriptor ReferencingProperty { get; internal set; }
		public EntityRelationshipType EntityRelationshipType { get; internal set; }
		
		public TypeRelationshipInfo(TypeDescriptor relatedTypeDescriptor, EntityRelationshipType entityRelationshipType, PropertyDescriptor relatedProperty)
		{
			this.RelatedTypeTypeDescriptor = relatedTypeDescriptor;
			this.EntityRelationshipType = entityRelationshipType;
			this.ReferencingProperty = relatedProperty;
		}
	}
}
