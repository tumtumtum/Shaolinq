// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System;
using NUnit.Framework;
using Shaolinq.Sqlite;
using Shaolinq.Tests.GenericModel;

namespace Shaolinq.Tests
{
	[TestFixture(Category = "IgnoreOnMono")]
	public class IdentityModelTests
	{
		private readonly IdentityModel model;
		private readonly ShaolinqIdentityUserStore<ShaolinqIdentityUser<Guid>, IdentityModel, Guid, DbUser, DbUserLogin, DbUserClaim, DbUserRole> userStore;

		public IdentityModelTests()
		{
			this.model = DataAccessModel.BuildDataAccessModel<IdentityModel>(SqliteConfiguration.Create(":memory:", null, BaseTests<IdentityModel>.useMonoData));
			this.model.Create(DatabaseCreationOptions.IfDatabaseNotExist);

			this.userStore = new ShaolinqIdentityUserStore<ShaolinqIdentityUser<Guid>, IdentityModel, Guid, DbUser, DbUserLogin, DbUserClaim, DbUserRole>(this.model);
		}

		[Test]
		public void Test()
		{
			var empty = Guid.Empty;

			this.userStore.FindById(empty);
		}
	}
}
