// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)
namespace Shaolinq.Tests.ComplexPrimaryKeyModel
{
	[DataAccessObject]
	public class Region
		: DataAccessObject<long>
	{
		[PrimaryKey]
		[PersistedMember]
		public virtual string Name { get; set; }

		[PersistedMember]
		public virtual double Range { get; set; }

		[PersistedMember]
		public virtual double Diameter { get; set; }

		[PersistedMember]
		public virtual Coordinate Center { get; set; }
	}
}
