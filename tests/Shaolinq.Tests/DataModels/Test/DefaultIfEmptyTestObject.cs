namespace Shaolinq.Tests.DataModels.Test
{
	[DataAccessObject]
	public abstract class DefaultIfEmptyTestObject
		: DataAccessObject<int>
	{
		[PersistedMember]
		public abstract string Text { get; set; }

		[PersistedMember]
		public abstract int Integer { get; set; }

		[PersistedMember]
		public abstract int? NullableInteger { get; set; }
	}
}
