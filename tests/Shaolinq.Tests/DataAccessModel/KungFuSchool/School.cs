namespace Shaolinq.Tests.DataAccessModel.KungFuSchool
{
	[DataAccessObject]
	public abstract class School
		: DataAccessObject<long>
	{
		[PersistedMember]
		public abstract string Name { get; set; }

		[RelatedDataAccessObjects]
		public abstract RelatedDataAccessObjects<Student> Students { get; }
	}
}
