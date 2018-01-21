// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using NUnit.Framework;
using Shaolinq.Tests.TestModel;

namespace Shaolinq.Tests
{
	[TestFixture("MySql")]
	[TestFixture("Postgres")]
	[TestFixture("Postgres.DotConnect")]
	[TestFixture("Postgres.DotConnect.Unprepared")]
	[TestFixture("SqlServer")]
	[TestFixture("Sqlite")]
	[TestFixture("SqliteInMemory")]
	[TestFixture("SqliteClassicInMemory")]
	public class ConcurrencyTests
		: BaseTests<TestDataAccessModel>
	{
		public ConcurrencyTests(string providerName)
			: base(providerName)
		{
			try
			{
				using (var scope = new TransactionScope())
				{
					var school = this.model.Schools.Create();

					school.Name = "Lee's Kung Fu School";

					scope.Complete();
				}
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
			}
		}

		[Test]
		public void Test_Hammer_Get_Changed_Properties()
		{
			object x = 10;
			var values = new ObjectPropertyValue[1];

			values[0] = new ObjectPropertyValue(typeof(string), "", "", 0, x);

			for (var i = 0; i < 200; i++)
			{
				using (var scope = new TransactionScope())
				{
					var school = this.model.GetDataAccessObjects<School>().Create();

					school.Name = "Lee's Kung Fu School";
					
					scope.Complete();
				}
			}
		}

		private Task<School[]> ReadAllSchools()
		{
			var schools = this.model.GetDataAccessObjects<School>().Where(c => c.Name != "ewoiuroi").ToArray();

			return Task.Run(() => schools);
		}

		private Task<List<School>> ReadAllSchoolsAsync()
		{
			return this.model.GetDataAccessObjects<School>().Where(c => c.Name != "ewoiuroi").ToListAsync();
		}

		[Test]
		public void Test_Query_On_Lots_Of_Threads_No_TransactionScope()
		{
			var exceptions = new List<Exception>();
			var threads = new List<Thread>();
			var random = new Random();

			for (var i = 0; i < 1; i++)
			{
				var action = (ThreadStart)delegate
				{
					try
					{
						for (var j = 0; j < 50; j++)
						{
							Thread.Sleep(random.Next(0, 5));

							this.model.GetDataAccessObjects<School>().Where(c => c.Name != "ewoiuroi").ToList();
						}
					}
					catch (Exception e)
					{
						lock (exceptions)
						{
							exceptions.Add(e);

							Console.WriteLine(e);
						}
					}
				};

				var thread = new Thread(action);

				threads.Add(thread);
			}

			threads.ForEach(c => c.Start());
			threads.ForEach(c => c.Join());

			if (exceptions.Count > 0)
			{
				exceptions.ForEach(Console.WriteLine);
			}

			Assert.AreEqual(0, exceptions.Count);

			exceptions.ForEach(Console.WriteLine);
		}

		[Test]
		public void Test_Query_On_Lots_Of_Threads_No_TransactionScopeAsync()
		{
			var exceptions = new List<Exception>();
			var threads = new List<Thread>();
			var random = new Random();

			for (var i = 0; i < 10; i++)
			{
				var action = (ThreadStart)async delegate
				{
					try
					{
						for (var j = 0; j < 50; j++)
						{
							Thread.Sleep(random.Next(0, 5));

							await this.ReadAllSchoolsAsync();
						}
					}
					catch (Exception e)
					{
						lock (exceptions)
						{
							exceptions.Add(e);

							Console.WriteLine(e);
						}
					}
				};

				var thread = new Thread(action);

				threads.Add(thread);
			}

			threads.ForEach(c => c.Start());
			threads.ForEach(c => c.Join());

			if (exceptions.Count > 0)
			{
				exceptions.ForEach(Console.WriteLine);
			}

			Assert.AreEqual(0, exceptions.Count);

			exceptions.ForEach(Console.WriteLine);
		}
	}
}
