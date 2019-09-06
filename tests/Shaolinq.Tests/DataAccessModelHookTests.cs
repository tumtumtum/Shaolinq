using System;
using System.Linq;
using System.Transactions;
using NUnit.Framework;
using Shaolinq.Tests.ComplexPrimaryKeyModel;
using Shaolinq.Tests.TestModel;

namespace Shaolinq.Tests
{
	[TestFixture("MySql")]
	[TestFixture("Sqlite")]
	[TestFixture("SqliteInMemory")]
	[TestFixture("SqliteClassicInMemory")]
	[TestFixture("Sqlite:DataAccessScope")]
	[TestFixture("SqlServer:DataAccessScope")]
	[TestFixture("Postgres")]
	[TestFixture("Postgres.DotConnect")]
	public class DataAccessModelHookTests : BaseTests<TestDataAccessModel>
	{
		public class TestDataModelHook : DataAccessModelHookBase
		{
			public int AfterSubmitCallCompleteCount { get; private set; }

			public override void Create(DataAccessObject dataAccessObject)
			{
				Console.WriteLine("Create");

				base.Create(dataAccessObject);
			}

			public override void AfterSubmit(DataAccessModelHookSubmitContext context)
			{
				Console.WriteLine($"AfterSubmit - IsCommit: {context.IsCommit}, new objects: {context.New.Count()} {context.Exception?.Message}");

				if (context.IsCommit)
				{
					this.AfterSubmitCallCompleteCount++;
				}

				base.AfterSubmit(context);
			}
		}

		private TestDataModelHook testDataModelHook;
		
		public DataAccessModelHookTests(string providerName) : base(providerName)
		{
		}

		[SetUp]
		public void SetUp()
		{
			this.testDataModelHook = new TestDataModelHook();
			this.model.AddHook(this.testDataModelHook);
		}

		[TearDown]
		public void TearDown()
		{
			this.model.RemoveHook(testDataModelHook);
		}

		[TestCase(false, false)]
		[TestCase(false, true)]
		[TestCase(true, false)]
		[TestCase(true, true)]
		public void Test_DataAccessScope_CreateFlushComplete_Calls_DataModelHook(bool flush, bool complete)
		{
			using (var scope = DataAccessScope.CreateReadCommitted())
			{
				var cat = this.model.Cats.Create();

				Console.WriteLine("===");

				if (flush) scope.Flush();
				if (complete) scope.Complete();
			}

			Assert.AreEqual(this.testDataModelHook.AfterSubmitCallCompleteCount, complete ? 1 : 0);
		}

		[TestCase(false, false)]
		[TestCase(false, true)]
		[TestCase(true, false)]
		[TestCase(true, true)]
		public void Test_TransactionScope_CreateFlushComplete_Calls_DataModelHook(bool flush, bool complete)
		{
			using (var scope = new TransactionScope())
			{
				var cat = this.model.Cats.Create();

				Console.WriteLine("===");

				if (flush) scope.Flush();
				if (complete) scope.Complete();
			}

			Assert.AreEqual(this.testDataModelHook.AfterSubmitCallCompleteCount, complete ? 1 : 0);
		}


		[TestCase(false, false)]
		[TestCase(false, true)]
		[TestCase(true, false)]
		[TestCase(true, true)]
		public void Test_Distributed_Transaction_DataAccessScope_CreateFlushComplete_Calls_DataModelHook(bool flush, bool complete)
		{
			var config2 = this.CreateSqliteClassicInMemoryConfiguration(null);
			var model2 = DataAccessModel.BuildDataAccessModel<ComplexPrimaryKeyDataAccessModel>(config2);
			var hook2 = new TestDataModelHook();
			model2.AddHook(hook2);
			model2.Create(DatabaseCreationOptions.IfDatabaseNotExist);

			using (var scope = new DataAccessScope())
			{
				var cat = this.model.Cats.Create();
				var coord = model2.Coordinates.Create(1);

				Console.WriteLine("===");

				if (flush) scope.Flush();
				if (complete) scope.Complete();
			}

			Assert.AreEqual(this.testDataModelHook.AfterSubmitCallCompleteCount, complete ? 1 : 0);
			Assert.AreEqual(hook2.AfterSubmitCallCompleteCount, complete ? 1 : 0);
		}

		[TestCase(false, false)]
		[TestCase(false, true)]
		[TestCase(true, false)]
		[TestCase(true, true)]
		public void Test_Distributed_Transaction_TransactionScope_CreateFlushComplete_Calls_DataModelHook(bool flush, bool complete)
		{
			var config2 = this.CreateSqliteClassicInMemoryConfiguration(null);
			var model2 = DataAccessModel.BuildDataAccessModel<ComplexPrimaryKeyDataAccessModel>(config2);
			var hook2 = new TestDataModelHook();
			model2.AddHook(hook2);
			model2.Create(DatabaseCreationOptions.IfDatabaseNotExist);

			using (var scope = new TransactionScope())
			{
				var cat = this.model.Cats.Create();
				var coord = model2.Coordinates.Create(1);

				Console.WriteLine("===");

				if (flush) scope.Flush();
				if (complete) scope.Complete();
			}

			Assert.AreEqual(this.testDataModelHook.AfterSubmitCallCompleteCount, complete ? 1 : 0);
			Assert.AreEqual(hook2.AfterSubmitCallCompleteCount, complete ? 1 : 0);
		}

		[TestCase(false, false)]
		[TestCase(false, true)]
		[TestCase(true, false)]
		[TestCase(true, true)]
		public void Test_Nested_DataAccessScope_Inner_Complete_Calls_DataModelHook(bool flush, bool complete)
		{
			using (var outerScope = new DataAccessScope())
			{
				var cat1 = this.model.Cats.Create();

				using (var innerScope = new DataAccessScope())
				{
					var cat2 = this.model.Cats.Create();

					innerScope.Complete();
				}

				var cat3 = this.model.Cats.Create();

				if (flush) outerScope.Flush();
				if (complete) outerScope.Complete();
			}

			Assert.AreEqual(this.testDataModelHook.AfterSubmitCallCompleteCount, complete ? 1 : 0);
		}

		[Test]
		public void Test_Nested_DataAccessScope_Inner_Not_Complete_Should_Throw_TransactionAbortedException()
		{
			using (var outerScope = new DataAccessScope())
			{
				var cat1 = this.model.Cats.Create();

				using (var innerScope = new DataAccessScope())
				{
					var cat2 = this.model.Cats.Create();
				}

				Assert.Throws<TransactionAbortedException>(() =>
				{
					var cat3 = this.model.Cats.Create();
				});
			}
		}

		[TestCase(false, false)]
		[TestCase(false, true)]
		[TestCase(true, false)]
		[TestCase(true, true)]
		public void Test_Nested_TransactionScope_Inner_Complete_Calls_DataModelHook(bool flush, bool complete)
		{
			using (var outerScope = new TransactionScope())
			{
				var cat1 = this.model.Cats.Create();

				using (var innerScope = new TransactionScope())
				{
					var cat2 = this.model.Cats.Create();

					innerScope.Complete();
				}

				var cat3 = this.model.Cats.Create();

				if (flush) outerScope.Flush();
				if (complete) outerScope.Complete();
			}

			Assert.AreEqual(this.testDataModelHook.AfterSubmitCallCompleteCount, complete ? 1 : 0);
		}

		[Test]
		public void Test_Nested_TransactionScope_Inner_Not_Complete_Should_Throw_TransactionAbortedException()
		{
			using (var outerScope = new TransactionScope())
			{
				var cat1 = this.model.Cats.Create();

				using (var innerScope = new TransactionScope())
				{
					var cat2 = this.model.Cats.Create();
				}

				Assert.Throws<TransactionAbortedException>(() =>
				{
					var cat3 = this.model.Cats.Create();
				});
			}
		}
	}
}