// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)
namespace Shaolinq.Tests.ComplexPrimaryKeyModel
{
	[DataAccessObject]
	public abstract class Region
		: DataAccessObject<long>
	{
		[PrimaryKey]
		[PersistedMember]
		public abstract string Name { get; set; }

		[PersistedMember]
		public abstract double Range { get; set; }

		[PersistedMember]
		public abstract double Diameter { get; set; }

		[PersistedMember]
		public abstract Coordinate Center { get; set; }
	}
}
