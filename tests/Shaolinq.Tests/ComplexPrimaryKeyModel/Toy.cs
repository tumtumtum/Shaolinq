// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System;
using Platform.Validation;

namespace Shaolinq.Tests.ComplexPrimaryKeyModel
{
	[DataAccessObject]
	public abstract class Toy
		: DataAccessObject<Guid>
	{
		[BackReference]
		[ValueRequired]
		public abstract Child Owner { get; set; }

		[PersistedMember]
		public abstract string Name { get; set; }

		[PersistedMember]
		public abstract bool? Missing { get; set; }
	}
}
