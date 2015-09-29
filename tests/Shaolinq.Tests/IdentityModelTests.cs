using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Shaolinq.Sqlite;
using Shaolinq.Tests.GenericModel;

namespace Shaolinq.Tests
{
	[TestFixture(Category = "IgnoreOnMono")]
	public class IdentityModelTests
	{
		private IdentityModel model;
		private ShaolinqIdentityUserStore<ShaolinqIdentityUser<Guid>, IdentityModel, Guid, DbUser, DbUserLogin, DbUserClaim, DbUserRole> userStore;

		public IdentityModelTests()
		{
			model = DataAccessModel.BuildDataAccessModel<IdentityModel>(SqliteConfiguration.Create(":memory:", null, BaseTests<IdentityModel>.useMonoData));
			model.Create(DatabaseCreationOptions.IfDatabaseNotExist);

			userStore = new ShaolinqIdentityUserStore<ShaolinqIdentityUser<Guid>, IdentityModel, Guid, DbUser, DbUserLogin, DbUserClaim, DbUserRole>(model);
		}

		[Test]
		public void Test()
		{
			var empty = Guid.Empty;

			userStore.FindById(empty);
		}
	}
}
