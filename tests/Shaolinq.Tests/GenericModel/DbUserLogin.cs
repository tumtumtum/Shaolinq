// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System;
using Platform.Validation;
using Shaolinq.Tests.GenericModel.Interfaces;

namespace Shaolinq.Tests.GenericModel
{
	[DataAccessObject(Name = "UserLogin")]
	public abstract class DbUserLogin : DataAccessObject<Guid>, IShaolinqIdentityDbUserLogin<Guid, DbUser>
	{
		[ValueRequired]
		[BackReference]
		public abstract DbUser User { get; set; }

		[PersistedMember]
		public abstract string LoginProvider { get; set; }

		[PersistedMember]
		public abstract string ProviderKey { get; set; }
	}
}