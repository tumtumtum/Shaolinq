// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)
namespace Shaolinq.Tests.ComplexPrimaryKeyModel
{
	[DataAccessObject]
	public abstract class Coordinate
		: DataAccessObject<long>
	{
		[PersistedMember]
		public abstract string Label { get; set; }

		[PersistedMember]
		public abstract double Longitude { get; set; }

		[PersistedMember]
		public abstract double Magnitude { get; set; }
	}
}
