using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
		public class VerifyChangesDataModelHook : DataAccessModelHookBase
		{
			public class DataAccessObjectChangeInfo : IEquatable<DataAccessObjectChangeInfo>
			{
				public string ObjectType { get; set; }
				public string ChangeType { get; set; }
				public bool HasChanged { get; set; }
				public bool IsCommitted { get; set; }
				public DataAccessObjectState ObjectState { get; set; }
				public IDictionary<string, string> ChangedPropertyValues { get; set; }

				public bool Equals(DataAccessObjectChangeInfo other)
				{
					if (ReferenceEquals(null, other)) return false;
					if (ReferenceEquals(this, other)) return true;
					return
						ObjectType == other.ObjectType &&
						ChangeType == other.ChangeType &&
						HasChanged == other.HasChanged &&
						//ObjectState == other.ObjectState &&
						ChangedPropertyValues.SequenceEqual(other.ChangedPropertyValues);
				}

				public override bool Equals(object obj)
				{
					if (ReferenceEquals(null, obj)) return false;
					if (ReferenceEquals(this, obj)) return true;
					if (obj.GetType() != this.GetType()) return false;
					return Equals((DataAccessObjectChangeInfo) obj);
				}

				public override int GetHashCode()
				{
					unchecked
					{
						var hashCode = (ObjectType != null ? ObjectType.GetHashCode() : 0);
						hashCode = (hashCode * 397) ^ (ChangeType != null ? ChangeType.GetHashCode() : 0);
						hashCode = (hashCode * 397) ^ HasChanged.GetHashCode();
						hashCode = (hashCode * 397) ^ ObjectState.GetHashCode();
						hashCode = (hashCode * 397) ^ (ChangedPropertyValues != null ? ChangedPropertyValues.GetHashCode() : 0);
						return hashCode;
					}
				}

				public override string ToString()
				{
					return $"{nameof(ObjectType)}: {ObjectType}, {nameof(ChangeType)}: {ChangeType}, {nameof(HasChanged)}: {HasChanged}, {nameof(ObjectState)}: {ObjectState}, {nameof(ChangedPropertyValues)}: {ChangedPropertyValues.Count}";
				}
			}

			public IDictionary<string, ICollection<DataAccessObjectChangeInfo>> BeforeSubmitChangeInfo { get; } = new Dictionary<string, ICollection<DataAccessObjectChangeInfo>>();
			public IDictionary<string, ICollection<DataAccessObjectChangeInfo>> AfterSubmitChangeInfo { get; } = new Dictionary<string, ICollection<DataAccessObjectChangeInfo>>();

			private void AssertThatBeforeAndAfterChangesMatch()
			{
				//Assert.That();
				Assert.AreEqual(this.BeforeSubmitChangeInfo, this.AfterSubmitChangeInfo);
			}

			private void Reset()
			{
				this.BeforeSubmitChangeInfo.Clear();
				this.AfterSubmitChangeInfo.Clear();

				Console.WriteLine($"{nameof(VerifyChangesDataModelHook)} Reset");
			}

			public override void BeforeSubmit(DataAccessModelHookSubmitContext context)
			{
				AddChangedObjects(context, this.BeforeSubmitChangeInfo);
				WriteChangedObjects("BeforeSubmit", this.BeforeSubmitChangeInfo, context.IsCommit);
			}

			public override Task BeforeSubmitAsync(DataAccessModelHookSubmitContext context, CancellationToken cancellationToken)
			{
				AddChangedObjects(context, this.BeforeSubmitChangeInfo);
				WriteChangedObjects("BeforeSubmit", this.BeforeSubmitChangeInfo, context.IsCommit);

				return Task.FromResult(0);
			}

			public override void AfterSubmit(DataAccessModelHookSubmitContext context)
			{
				AddChangedObjects(context, this.AfterSubmitChangeInfo);
				WriteChangedObjects("AfterSubmit", this.AfterSubmitChangeInfo, context.IsCommit);

				//AssertThatBeforeAndAfterChangesMatch();
				Reset();
			}

			public override Task AfterSubmitAsync(DataAccessModelHookSubmitContext context, CancellationToken cancellationToken)
			{
				AddChangedObjects(context, this.AfterSubmitChangeInfo);
				WriteChangedObjects("AfterSubmit", this.AfterSubmitChangeInfo, context.IsCommit);

				//AssertThatBeforeAndAfterChangesMatch();
				Reset();

				return Task.FromResult(0);
			}

			private void AddChangedObjects(DataAccessModelHookSubmitContext context, IDictionary<string, ICollection<DataAccessObjectChangeInfo>> changeInfosByType)
			{
				AddChangedObjects(context.New, "New", changeInfosByType);
				AddChangedObjects(context.Updated, "Updated", changeInfosByType);
				AddChangedObjects(context.Deleted, "Deleted", changeInfosByType);
			}

			private void AddChangedObjects(
				IEnumerable<DataAccessObject> objects,
				string changeType,
				IDictionary<string, ICollection<DataAccessObjectChangeInfo>> changeInfos)
			{
				ICollection<DataAccessObjectChangeInfo> changeInfo;
				if (!changeInfos.TryGetValue(changeType, out changeInfo))
				{
					changeInfo = new List<DataAccessObjectChangeInfo>();
					changeInfos.Add(changeType, changeInfo);
				}

				foreach (var obj in objects)
				{
					changeInfo.Add(new DataAccessObjectChangeInfo
					{
						ObjectType = obj.ToString(),
						ChangeType = changeType,
						HasChanged = obj.GetAdvanced().HasObjectChanged,
						IsCommitted = obj.GetAdvanced().IsCommitted,
						ObjectState = obj.GetAdvanced().ObjectState,
						ChangedPropertyValues = obj
							.GetAdvanced()
							.GetChangedProperties()
							.ToDictionary(x => x.PropertyName, x => x.Value.ToString())
					});
				}
			}

			private void WriteChangedObjects(string hook, IDictionary<string, ICollection<DataAccessObjectChangeInfo>> changeInfosByType, bool isCommit)
			{
				Console.WriteLine($"{hook} (Commit: {isCommit})");

				foreach (var changeInfos in changeInfosByType)
				{
					Console.WriteLine($" - {changeInfos.Key}:");

					foreach (var changeInfo in changeInfos.Value)
					{
						Console.WriteLine($"   - {changeInfo.ObjectType} (HasChanged: {changeInfo.HasChanged}, ObjectState: {changeInfo.ObjectState}, IsCommitted: {changeInfo.IsCommitted}):");

						foreach (var changedProp in changeInfo.ChangedPropertyValues)
						{
							Console.WriteLine($"     - {changedProp.Key} => {changedProp.Value}");
						}
					}
				}
			}
		}

		private VerifyChangesDataModelHook verifyChangesDataModelHook;

		public DataAccessModelHookChangedPropertyTests(string providerName) : base(providerName)
		{
		}

		[SetUp]
		public void SetUp()
		{
			this.verifyChangesDataModelHook = new VerifyChangesDataModelHook();
			this.model.AddHook(this.verifyChangesDataModelHook);
		}

		[TearDown]
		public void TearDown()
		{
			this.model.RemoveHook(this.verifyChangesDataModelHook);
		}

		[Test]
		public void Test_Changed_Properties_Related_Object()
		{
			using (var scope = new DataAccessScope())
			{
				var obj = this.model.ObjectWithRelatedObjects.Create(1);

				//obj.Id = 1;
				obj.Name = "Parent";
				obj.RelatedObject = this.model.ObjectWithRelatedObjects.Create(2);
				//obj.RelatedObject.Id = 2;
				obj.RelatedObject.Name = "Child";

				scope.Complete();
			}

			Console.WriteLine("Updating object");

			using (var scope = new DataAccessScope())
			{
				var obj = this.model.ObjectWithRelatedObjects.First(x => x.Id == 1);

				obj.Name = "ParentModified";
				obj.RelatedObject.Name = "ChildModified";

				scope.Complete();
			}
		}

		[Test]
		public void Test_Changed_Properties_BackReference()
		{
			using (var scope = new DataAccessScope())
			{
				var obj = this.model.ObjectWithBackReferences.Create(1);

				//obj.Id = 1;
				obj.Name = "Parent";
				obj.RelatedObject = this.model.ObjectWithBackReferences.Create(2);
				//obj.RelatedObject.Id = 2;
				obj.RelatedObject.Name = "Child";

				scope.Complete();
			}

			Console.WriteLine("Updating object");

			using (var scope = new DataAccessScope())
			{
				var obj = this.model.ObjectWithBackReferences.First(x => x.Id == 1);

				obj.Name = "ParentModified";
				obj.RelatedObject.Name = "ChildModified";

				scope.Complete();
			}
		}

		[Test]
		public void Test_Changed_Properties_Computed_Text_Member()
		{
			long id;

			using (var scope = new DataAccessScope())
			{
				var obj = this.model.ObjectWithComputedTextMembers.Create();
				obj.Name = "Foo";

				scope.Flush();

				id = obj.Id;

				scope.Complete();
			}

			using (var scope = new DataAccessScope())
			{
				var obj = this.model.ObjectWithComputedTextMembers.GetByPrimaryKey(id);

				Console.WriteLine($"ServerGeneratedUrn: {obj.ServerGeneratedUrn}");
				Console.WriteLine($"NonServerGeneratedUrn: {obj.NonServerGeneratedUrn}");
			}
		}

		[Test]
		public void Test_Changed_Properties_Computed_Member()
		{
			long id;

			using (var scope = new DataAccessScope())
			{
				var obj = this.model.ObjectWithComputedMembers.Create();
				obj.Number = 5;

				scope.Flush();

				id = obj.Id;

				scope.Complete();
			}

			using (var scope = new DataAccessScope())
			{
				var obj = this.model.ObjectWithComputedMembers.GetByPrimaryKey(id);

				Console.WriteLine($"Id: {obj.Id}, MutatedId: {obj.MutatedId}");
				Console.WriteLine($"Number: {obj.Number}, MutatedNumber: {obj.MutatedNumber}");
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

			Console.WriteLine("Updating object");

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

				scope.Flush();

				scope.Complete();
			}

			//this.verifyChangesDataModelHook.AssertThatBeforeAndAfterChangesMatch();
			//this.verifyChangesDataModelHook.Reset();

			Console.WriteLine("Updating object");

			using (var scope = new DataAccessScope())
			{
				var obj = this.model.ObjectWithGuidNonAutoIncrementPrimaryKeys.GetByPrimaryKey(id);

				obj.Id = Guid.NewGuid();

				scope.Complete();
			}

			//this.verifyChangesDataModelHook.AssertThatBeforeAndAfterChangesMatch();
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

				id = obj.Id;

				Console.WriteLine("Completing");
				scope.Complete();
				Console.WriteLine("Leaving scope");
			}

			Console.WriteLine("Left scope");

			//this.verifyChangesDataModelHook.AssertThatBeforeAndAfterChangesMatch();
			//this.verifyChangesDataModelHook.Reset();

			Console.WriteLine("=================================================================");

			using (var scope = new DataAccessScope())
			{
				var obj = this.model.ObjectWithGuidAutoIncrementPrimaryKeys.GetByPrimaryKey(id);

				obj.Id = Guid.NewGuid();

				scope.Complete();
			}

			//this.verifyChangesDataModelHook.AssertThatBeforeAndAfterChangesMatch();
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

			//this.verifyChangesDataModelHook.AssertThatBeforeAndAfterChangesMatch();
			//this.verifyChangesDataModelHook.Reset();

			Console.WriteLine("Updating object");
			
			using (var scope = new DataAccessScope())
			{
				var obj = this.model.ObjectWithLongNonAutoIncrementPrimaryKeys.GetByPrimaryKey(id);

				obj.Id = 200L;
				obj.Name = $"{Guid.NewGuid()}";

				scope.Complete();
			}

			//this.verifyChangesDataModelHook.AssertThatBeforeAndAfterChangesMatch();
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

			//this.verifyChangesDataModelHook.AssertThatBeforeAndAfterChangesMatch();
			//this.verifyChangesDataModelHook.Reset();

			Console.WriteLine("Updating object");

			using (var scope = new DataAccessScope())
			{
				var obj = this.model.ObjectWithLongAutoIncrementPrimaryKeys.GetByPrimaryKey(id);

				obj.Id = 200L;
				obj.Name = $"{Guid.NewGuid()}";

				scope.Complete();
			}

			//this.verifyChangesDataModelHook.AssertThatBeforeAndAfterChangesMatch();
		}

		[Test]
		public void Test_Changed_Properties_Delete()
		{
			Guid id;

			using (var scope = new DataAccessScope())
			{
				var obj = this.model.ObjectWithGuidAutoIncrementPrimaryKeys.Create();

				scope.Complete();

				id = obj.Id;
			}

			using (var scope = new DataAccessScope())
			{
				var obj = this.model.ObjectWithGuidAutoIncrementPrimaryKeys.GetReference(id);

				obj.Delete();

				scope.Complete();
			}
		}

		[Test]
		public void Test_Deflated_Object_Hook()
		{
			long id;

			using (var scope = new DataAccessScope())
			{
				var cat = this.model.Cats.Create();

				scope.Flush();

				id = cat.Id;

				scope.Complete();
			}

			Console.WriteLine("Updating deflated object");

			using (var scope = new DataAccessScope())
			{
				var cat = this.model.Cats.GetReference(id);

				cat.Name = "NewCat";

				scope.Complete();
			}
		}

		[Test]
		public void Test_DeflatedPredicate_Object_Hook()
		{
			long id;

			using (var scope = new DataAccessScope())
			{
				var cat = this.model.Cats.Create();

				scope.Flush();

				id = cat.Id;

				scope.Complete();
			}

			Console.WriteLine("Updating deflated object");

			using (var scope = new DataAccessScope())
			{
				var cat = this.model.Cats.GetReference(c => c.Id == id);

				cat.Name = "NewCat";

				scope.Complete();
			}
		}
	}
}