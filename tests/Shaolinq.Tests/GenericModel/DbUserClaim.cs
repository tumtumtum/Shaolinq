// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)

using System;
using Platform.Validation;
using Shaolinq.Tests.GenericModel.Interfaces;

namespace Shaolinq.Tests.GenericModel
{
	[DataAccessObject(Name = "UserClaim")]
	public abstract class DbUserClaim : DataAccessObject<Guid>, IShaolinqIdentityDbUserClaim<Guid, DbUser>
	{
		[ValueRequired]
		[BackReference]
		public abstract DbUser User { get; set; }

		[PersistedMember]
		public abstract string ClaimType { get; set; }

		[PersistedMember]
		public abstract string ClaimValue { get; set; }
	}
}