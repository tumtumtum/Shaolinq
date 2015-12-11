using System;

namespace Shaolinq.Tests.ComplexPrimaryKeyModel
{
	[DataAccessObject]
	public abstract class SuperMall
		: DataAccessObject<Mall>
	{
		//public abstract override Mall Id { get; set; }

		[PrimaryKey]
		[PersistedMember]
		public abstract Address Address1 { get; set; }

		[PrimaryKey]
		[PersistedMember]
		public abstract Address Address2 { get; set; }
	}
}