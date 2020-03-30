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
}