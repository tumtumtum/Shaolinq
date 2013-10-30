namespace Shaolinq.Tests.DataAccessModel.Test
{
	[DataAccessObject]
	public abstract class ObjectWithCompositePrimaryKey
		: DataAccessObject<long>
	{
		[AutoIncrement(false)]
		public abstract override long Id { get; set; }

		[PrimaryKey, PersistedMember]
		public abstract string SecondaryKey { get; set; }

		[PersistedMember]
		public abstract string Name { get; set; }
	}
}
