// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using Platform.Validation;

namespace Shaolinq.Tests.TestModel
{
	[DataAccessObject]
	[Index("Name", "Id:IncludeOnly", IndexName = "name_id_idx")]
	public abstract class ObjectWithCompositePrimaryKey
		: DataAccessObject<long>
	{
		[AutoIncrement(false)]
		public abstract override long Id { get; set; }

		[PrimaryKey, PersistedMember, SizeConstraint(MaximumLength = 128)]
		public abstract string SecondaryKey { get; set; }
	
		[PrimaryKey]
		[PersistedMember]
		public abstract Student Student { get; set; }

		[PersistedMember]
		public abstract string Name { get; set; }
	}
}
