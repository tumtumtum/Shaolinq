// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System;
using Platform.Validation;
using Shaolinq.Tests.GenericModel.Interfaces;

namespace Shaolinq.Tests.GenericModel
{
	[DataAccessObject(Name = "UserRole")]
	[Index("User", "Role", Unique = true)]
	public abstract class DbUserRole : DataAccessObject<Guid>, IShaolinqIdentityDbUserRole<Guid, DbUser>
	{
		[ValueRequired]
		[BackReference]
		public abstract DbUser User { get; set; }

		[ValueRequired]
		[PersistedMember]
		public abstract string Role { get; set; }
	}
}