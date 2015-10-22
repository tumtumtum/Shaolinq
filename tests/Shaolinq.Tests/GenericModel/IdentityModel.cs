// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System;
using Shaolinq.Tests.GenericModel.Interfaces;

namespace Shaolinq.Tests.GenericModel
{
	[DataAccessModel]
	public abstract class IdentityModel
		: DataAccessModel, IShaolinqIdentityDataAccessModel<Guid, DbUser, DbUserLogin, DbUserClaim, DbUserRole>
	{
		[DataAccessObjects]
		public abstract DataAccessObjects<DbUser> Users { get; }

		[DataAccessObjects]
		public abstract DataAccessObjects<DbUserLogin> UserLogins { get; }
		
		[DataAccessObjects]
		public abstract DataAccessObjects<DbUserClaim> UserClaims { get; }

		[DataAccessObjects]
		public abstract DataAccessObjects<DbUserRole> UserRoles { get; }
	}
}
