// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System;
using Platform.Validation;

namespace Shaolinq.Tests.ComplexPrimaryKeyModel
{
	[DataAccessObject]
	public abstract class Child
		: DataAccessObject<Guid>
	{
		[PersistedMember, DefaultValue(true)]
		public abstract bool Good { get; set; }

		[PersistedMember]
		public abstract string Nickname { get; set; }

		[RelatedDataAccessObjects]
		public abstract RelatedDataAccessObjects<Toy> Toys { get; }
	}
}
