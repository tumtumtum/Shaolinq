// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)
namespace Shaolinq.Persistence
{
	public enum RelationshipType
	{
		None,
		OneToOne,
		ChildOfOneToMany,
		ParentOfOneToMany,
		ManyToMany
	}
}
