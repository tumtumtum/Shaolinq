// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)

using Platform.Validation;

namespace Shaolinq.Tests.ComplexPrimaryKeyModel
{
	[DataAccessObject]
	public abstract class Coordinate
		: DataAccessObject<long>
	{
		[PersistedMember]
		public abstract string Label { get; set; }

		[PersistedMember, DefaultValue(0)]
		public abstract double Longitude { get; set; }

		[PersistedMember, DefaultValue(0)]
		public abstract double Magnitude { get; set; }
	}
}
