// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)

using System;
using Shaolinq.Tests.GenericModel.Interfaces;

namespace Shaolinq.Tests.GenericModel
{
	[DataAccessObject(Name = "User")]
	public abstract class DbUser : DataAccessObject<Guid>, IShaolinqIdentityDbUser<Guid>
	{
		[Index(Unique = true)]
		[PersistedMember]
		public abstract string UserName { get; set; }

		[PersistedMember]
		public abstract string Name { get; set; }

		[PersistedMember]
		[Index(Unique = true)]
		public abstract string Email { get; set; }

		[PersistedMember]
		public abstract bool EmailConfirmed { get; set; }

		[PersistedMember]
		public abstract string PasswordHash { get; set; }

		[PersistedMember]
		public abstract string SecurityStamp { get; set; }

		[PersistedMember]
		public abstract bool IsAnonymousUser { get; set; }

		[PersistedMember]
		public abstract DateTime ActivationDate { get; set; }

		[RelatedDataAccessObjects]
		public abstract RelatedDataAccessObjects<DbUserLogin> UserLogins { get; }

		[RelatedDataAccessObjects]
		public abstract RelatedDataAccessObjects<DbUserClaim> UserClaims { get; }

		[RelatedDataAccessObjects]
		public abstract RelatedDataAccessObjects<DbUserRole> UserRoles { get; }
	}
}