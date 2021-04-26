namespace Shaolinq.Tests.TestModel
{
	[DataAccessObject]
	public abstract class ObjectWithComputedTextMember
		: DataAccessObject<long>
	{
		[ComputedTextMember("urn:$(PERSISTED_TYPENAME:L):{Id}")]
		public abstract string ServerGeneratedUrn { get; set; }

		[PersistedMember]
		public abstract string Name { get; set; }

		[ComputedTextMember("urn:$(PERSISTED_TYPENAME:L):{Name}")]
		public abstract string NonServerGeneratedUrn { get; set; }
	}

	[DataAccessObject]
	public abstract class ObjectWithComputedMember
		: DataAccessObject<long>
	{
		[ComputedMember("Id + 10", "Id = value - 10")]
		public virtual long? MutatedId { get; set; }

		[PersistedMember]
		public abstract long Number { get; set; }

		[ComputedMember("Number + 100", "Number = value - 100")]
		public virtual long? MutatedNumber { get; set; }
	}
}