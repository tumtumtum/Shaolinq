namespace Shaolinq.Tests.OtherDataAccessObjects
{
	[DataAccessObject]
	public abstract class Apple
		: Fruit
	{
		[PersistedMember]
		public abstract float Quality { get; set; }
	}
}
