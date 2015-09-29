using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Shaolinq.Sqlite;
using Shaolinq.Tests.GenericModel;

namespace Shaolinq.Tests
{
	[TestFixture]
	public class IdentityModelTests
	{
		private IdentityModel model;
		private ShaolinqIdentityUserStore<ShaolinqIdentityUser<Guid>, IdentityModel, Guid, DbUser, DbUserLogin, DbUserClaim, DbUserRole> userStore;

		public IdentityModelTests()
		{
			model = DataAccessModel.BuildDataAccessModel<IdentityModel>(SqliteConfiguration.Create(":memory:", null));
			model.Create(DatabaseCreationOptions.IfDatabaseNotExist);

			userStore = new ShaolinqIdentityUserStore<ShaolinqIdentityUser<Guid>, IdentityModel, Guid, DbUser, DbUserLogin, DbUserClaim, DbUserRole>(model);
		}

		[Test]
		public void Test()
		{
			Guid empty = Guid.Empty;

			//var test = model.Users.SingleOrDefault(c => c.Id.Equals(empty));

			userStore.FindById(empty);
		}
	}
}
