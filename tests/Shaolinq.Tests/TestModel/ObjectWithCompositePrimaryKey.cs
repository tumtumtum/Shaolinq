// Copyright (c) 2007-2014 Thong Nguyen (tumtumtum@gmail.com)

using Platform.Validation;

namespace Shaolinq.Tests.TestModel
{
	[DataAccessObject]
	public abstract class ObjectWithCompositePrimaryKey
		: DataAccessObject<long>
	{
		[AutoIncrement(false)]
		public abstract override long Id { get; set; }

		[PrimaryKey, PersistedMember, SizeConstraint(MaximumLength = 128)]
		public abstract string SecondaryKey { get; set; }
	
		[Index]
		[PersistedMember]
		public abstract Student Student { get; set; }

		[PersistedMember]
		public abstract string Name { get; set; }
	}
}
