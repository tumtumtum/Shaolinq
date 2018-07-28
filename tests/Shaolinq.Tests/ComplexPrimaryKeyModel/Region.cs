// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)

using Platform.Validation;

namespace Shaolinq.Tests.ComplexPrimaryKeyModel
{
	[DataAccessObject]
	public class Region
		: DataAccessObject<long>
	{
		[PrimaryKey]
		[PersistedMember]
		public virtual string Name { get; set; }

		[PersistedMember, DefaultValue(0)]
		public virtual double Range { get; set; }

		[PersistedMember, DefaultValue(0)]
		public virtual double Diameter { get; set; }

		[PersistedMember]
		public virtual Coordinate Center { get; set; }
	}
}
