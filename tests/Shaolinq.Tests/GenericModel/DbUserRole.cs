// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System;
using Platform.Validation;
using Shaolinq.Tests.GenericModel.Interfaces;

namespace Shaolinq.Tests.GenericModel
{
	[DataAccessObject(Name = "UserRole")]
	public abstract class DbUserRole : DataAccessObject<Guid>, IShaolinqIdentityDbUserRole<Guid, DbUser>
	{
		[ValueRequired]
		[BackReference]
		[Index(IndexName = "User_Role_Idx", CompositeOrder = 0, Unique = true)]
		public abstract DbUser User { get; set; }

		[ValueRequired]
		[PersistedMember]
		[Index(IndexName = "User_Role_Idx", CompositeOrder = 1, Unique = true)]
		public abstract string Role { get; set; }
	}
}