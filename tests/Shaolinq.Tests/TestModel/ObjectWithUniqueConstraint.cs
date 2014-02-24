using Platform.Validation;

namespace Shaolinq.Tests.TestModel
{
	[DataAccessObject]
	public abstract class ObjectWithUniqueConstraint
		: DataAccessObject<long>
	{
		[PersistedMember, Unique]
		public abstract string Name { get; set; }
	}
}
