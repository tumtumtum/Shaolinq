namespace Shaolinq.Persistence
{
	public enum EntityRelationshipType
	{
		None,
		OneToOne,
		ChildOfOneToMany,
		ParentOfOneToMany,
		ManyToMany
	}
}