using Platform.Validation;

namespace Shaolinq.Tests.TestModel
{
	[DataAccessObject]
	public class DefaultsTestObject : DataAccessObject<long>
	{
		[PersistedMember]
		public virtual int IntValue { get; set; }

		[ValueRequired, PersistedMember]
		public virtual int IntValueWithValueRequired { get; set; }

		[PersistedMember]
		public virtual int? NullableIntValue { get; set; }

		[ValueRequired, PersistedMember]
		public virtual int? NullableIntValueWithValueRequired { get; set; }
	}
}
