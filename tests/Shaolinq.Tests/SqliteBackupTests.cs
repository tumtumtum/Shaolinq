// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)

using System.Linq;
using NUnit.Framework;
using Shaolinq.Tests.TestModel;

namespace Shaolinq.Tests
{
	[TestFixture("SqliteInMemory:DataAccessScope", Category = "IgnoreOnMono")]
	public class SqliteBackupTests
		: BaseTests<TestDataAccessModel>
	{
		public SqliteBackupTests(string providerName)
			: base(providerName)
		{
		}

		[Test]
		public void Test_Backup()	
		{
			using (var scope = NewTransactionScope())
			{
				var school = this.model.Schools.Create();

				school.Name = "School1";

				scope.Complete();
			}

			var backupFileModel = DataAccessModel.BuildDataAccessModel<TestDataAccessModel>(CreateSqliteConfiguration("backup.sql3"));

			this.model.Backup(backupFileModel);
			
			var backupModel = DataAccessModel.BuildDataAccessModel<TestDataAccessModel>(CreateSqliteClassicInMemoryConfiguration(null));

			backupFileModel.Backup(backupModel);

			Assert.AreEqual("School1", this.model.Schools.Select(c => c.Name).Single());

			using (var scope = NewTransactionScope())
			{
				var school = this.model.Schools.Create();

				school.Name = "School2";

				scope.Complete();
			}

			Assert.AreEqual("School2", this.model.Schools.Select(c => c.Name).Single(c => c== "School2"));

			using (var scope = NewTransactionScope())
			{
				var school = backupModel.Schools.Create();

				school.Name = "School3";

				school = backupModel.Schools.Create();

				school.Name = "School4";

				scope.Complete();
			}

			Assert.IsNull(this.model.Schools.Select(c => c.Name).FirstOrDefault(c => c == "School3"));
			Assert.IsNull(this.model.Schools.Select(c => c.Name).FirstOrDefault(c => c == "School4"));

			Assert.AreEqual("School3", backupModel.Schools.Select(c => c.Name).Single(c => c == "School3"));
			Assert.AreEqual("School4", backupModel.Schools.Select(c => c.Name).Single(c => c == "School4"));

			Assert.AreEqual(2, this.model.Schools.Count());
			Assert.AreEqual(3, backupModel.Schools.Count());
		}
	}
}
