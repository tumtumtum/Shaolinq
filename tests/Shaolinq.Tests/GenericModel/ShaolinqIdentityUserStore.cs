// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Linq;
using Shaolinq.Tests.GenericModel.Interfaces;

namespace Shaolinq.Tests.GenericModel
{
	public class ShaolinqIdentityUserStore<TIdentityUser, TDataModel, TPrimaryKey, TDbUser, TDbUserLogin, TDbUserClaim, TDbUserRole> 
		where TIdentityUser : ShaolinqIdentityUser<TPrimaryKey>, new()
		where TDataModel : DataAccessModel, IShaolinqIdentityDataAccessModel<TPrimaryKey, TDbUser, TDbUserLogin, TDbUserClaim, TDbUserRole>
		where TPrimaryKey : IEquatable<TPrimaryKey>
		where TDbUser : DataAccessObject, IShaolinqIdentityDbUser<TPrimaryKey>
		where TDbUserLogin : DataAccessObject, IShaolinqIdentityDbUserLogin<TPrimaryKey, TDbUser>
		where TDbUserClaim : DataAccessObject, IShaolinqIdentityDbUserClaim<TPrimaryKey, TDbUser>
		where TDbUserRole : DataAccessObject, IShaolinqIdentityDbUserRole<TPrimaryKey, TDbUser>
	{
		private readonly TDataModel dataModel;

		public ShaolinqIdentityUserStore(TDataModel dataModel)
		{
			this.dataModel = dataModel;
		}

		public TDbUser FindById(TPrimaryKey userId)
		{
			return this.dataModel.Users.SingleOrDefault(x => x.Id.Equals(userId));
		}
	}
}
