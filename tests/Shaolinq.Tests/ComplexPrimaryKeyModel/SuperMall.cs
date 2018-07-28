// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)
namespace Shaolinq.Tests.ComplexPrimaryKeyModel
{
	[DataAccessObject]
	public class SuperMall
		: DataAccessObject<Mall>
	{
		[PrimaryKey]
		[PersistedMember]
		public virtual Address Address1 { get; set; }

		[PrimaryKey]
		[PersistedMember]
		public virtual Address Address2 { get; set; }
	}
}