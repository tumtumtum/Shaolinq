using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Shaolinq.Tests.TestModel;

namespace Shaolinq.Tests
{
	//[TestFixture("MySql")]
	//[TestFixture("Sqlite")]
	[TestFixture("SqliteInMemory")]
	//[TestFixture("SqliteClassicInMemory")]
	//[TestFixture("Sqlite:DataAccessScope")]
	//[TestFixture("SqlServer:DataAccessScope")]
	[TestFixture("Postgres")]
	//[TestFixture("Postgres.DotConnect")]
	public class DataAccessModelHookChangedPropertyTests : BaseTests<TestDataAccessModel>
	{
		public class WriteChangesDataModelHook : DataAccessModelHookBase
		{
			public IEnumerable<string> BeforeSubmitChangedPropertyNames { get; private set; }
			public IEnumerable<string> AfterSubmitChangedPropertyNames { get; private set; }

			public override void BeforeSubmit(DataAccessModelHookSubmitContext context)
			{
				Console.WriteLine();
				Console.WriteLine($"BeforeSubmit changes - commit: {context.IsCommit}");
				WriteChanges("New", context.New);
				WriteChanges("Updated", context.Updated);
				WriteChanges("Deleted", context.Deleted);
				Console.WriteLine();

				this.BeforeSubmitChangedPropertyNames =
					context.New.SelectMany(x => x.GetChangedProperties().Select(p => $"{x} {p.PropertyName}"))
						.Concat(context.Updated.SelectMany(x => x.GetChangedProperties().Select(p => $"{x} {p.PropertyName}")))
						.Concat(context.Deleted.SelectMany(x => x.GetChangedProperties().Select(p => $"{x} {p.PropertyName}")))
						.ToList();
			}

			public override void AfterSubmit(DataAccessModelHookSubmitContext context)
			{
				Console.WriteLine();
				Console.WriteLine($"AfterSubmit changes - commit: {context.IsCommit}");
				WriteChanges("New", context.New);
				WriteChanges("Updated", context.Updated);
				WriteChanges("Deleted", context.Deleted);
				Console.WriteLine();

				this.AfterSubmitChangedPropertyNames =
					context.New.SelectMany(x => x.GetChangedProperties().Select(p => $"{x} {p.PropertyName}"))
						.Concat(context.Updated.SelectMany(x => x.GetChangedProperties().Select(p => $"{x} {p.PropertyName}")))
						.Concat(context.Deleted.SelectMany(x => x.GetChangedProperties().Select(p => $"{x} {p.PropertyName}")))
						.ToList();
			}

			private static void WriteChanges(string state, IEnumerable<DataAccessObject> objects)
			{
				Console.WriteLine($"{state} ({objects.Count()})");
				foreach (var obj in objects)
				{
					WriteObjectState(obj);
				}
			}
		}

		private WriteChangesDataModelHook writeChangesDataModelHook;

		private static void WriteObjectState(DataAccessObject obj)
		{
			Console.WriteLine($" - {obj} (HasObjectChanged: {obj.GetAdvanced().HasObjectChanged}, ObjectState: {obj.GetAdvanced().ObjectState})");
			foreach (var change in obj.GetChangedProperties())
			{
				Console.WriteLine($"    - {change.PropertyName} => {change.Value}");
			}
		}

		public DataAccessModelHookChangedPropertyTests(string providerName) : base(providerName)
		{
		}

		[SetUp]
		public void SetUp()
		{
			this.writeChangesDataModelHook = new WriteChangesDataModelHook();
			this.model.AddHook(this.writeChangesDataModelHook);
		}

		[TearDown]
		public void TearDown()
		{
			this.model.RemoveHook(writeChangesDataModelHook);
		}

		[Test]
		public void Test_Changed_Properties()
		{
			using (var scope = new DataAccessScope())
			{
				var obj = this.model.Cats.Create();

				obj.Name = "Cat1";
				obj.Parent = this.model.Cats.Create();
				obj.Parent.Name = "ParentCat";

				scope.Complete();
			}

			using (var scope = new DataAccessScope())
			{
				var obj = this.model.Cats.First(x => x.Name == "Cat1");

				obj.Name = "Cat1Modified";
				obj.Parent.Name = "ParentCatModified";

				scope.Complete();
			}
		}

		[Test]
		[Ignore("Bug")]
		public void Test_Changed_Properties_Guid_NonAutoIncrement_Bug()
		{
			var id = Guid.NewGuid();

			using (var scope = new DataAccessScope())
			{
				var obj = this.model.ObjectWithGuidNonAutoIncrementPrimaryKeys.Create(id);

				scope.Complete();
			}

			using (var scope = new DataAccessScope())
			{
				var obj = this.model.ObjectWithGuidNonAutoIncrementPrimaryKeys.GetReference(id); // BUG doing this creates a SQL statement which tries to fetch the row using the new Id, not the old Id

				obj.Id = Guid.NewGuid();

				scope.Complete();
			}
		}

		[Test]
		public void Test_Changed_Properties_Guid_NonAutoIncrement()
		{
			var id = Guid.NewGuid();

			using (var scope = new DataAccessScope())
			{
				var obj = this.model.ObjectWithGuidNonAutoIncrementPrimaryKeys.Create(id);

				scope.Complete();
			}

			using (var scope = new DataAccessScope())
			{
				var obj = this.model.ObjectWithGuidNonAutoIncrementPrimaryKeys.GetByPrimaryKey(id);

				obj.Id = Guid.NewGuid();

				scope.Complete();
			}
		}

		[Test]
		public void Test_Changed_Properties_Guid_AutoIncrement()
		{
			Guid id;

			Console.WriteLine("Starting scope");

			using (var scope = new DataAccessScope())
			{
				Console.WriteLine("Creating object");
				var obj = this.model.ObjectWithGuidAutoIncrementPrimaryKeys.Create();
				Console.WriteLine("Created object");

				Console.WriteLine("Flushing");
				scope.Flush();
				Console.WriteLine("Flushed");

				WriteObjectState(obj);

				id = obj.Id;

				Console.WriteLine("Completing");
				scope.Complete();
				Console.WriteLine("Leaving scope");
			}

			Console.WriteLine("Left scope");

			Console.WriteLine("=================================================================");

			using (var scope = new DataAccessScope())
			{
				var obj = this.model.ObjectWithGuidAutoIncrementPrimaryKeys.GetByPrimaryKey(id);

				obj.Id = Guid.NewGuid();

				scope.Complete();
			}
		}

		[Test]
		public void Test_Changed_Properties_Long_NonAutoIncrement()
		{
			var id = 100L;

			using (var scope = new DataAccessScope())
			{
				var obj = this.model.ObjectWithLongNonAutoIncrementPrimaryKeys.Create(id);

				//obj.Name = $"{Guid.NewGuid()}";

				scope.Complete();
			}

			using (var scope = new DataAccessScope())
			{
				var obj = this.model.ObjectWithLongNonAutoIncrementPrimaryKeys.GetByPrimaryKey(id);

				obj.Id = 200L;
				obj.Name = $"{Guid.NewGuid()}";

				scope.Complete();
			}
		}

		[Test]
		public void Test_Changed_Properties_Long_AutoIncrement()
		{
			long id;

			using (var scope = new DataAccessScope())
			{
				var obj = this.model.ObjectWithLongAutoIncrementPrimaryKeys.Create();
				//obj.Name = $"{Guid.NewGuid()}";

				scope.Flush();

				id = obj.Id;

				scope.Complete();
			}

			using (var scope = new DataAccessScope())
			{
				var obj = this.model.ObjectWithLongAutoIncrementPrimaryKeys.GetByPrimaryKey(id);

				obj.Id = 200L;
				obj.Name = $"{Guid.NewGuid()}";

				scope.Complete();
			}
		}
	}
}