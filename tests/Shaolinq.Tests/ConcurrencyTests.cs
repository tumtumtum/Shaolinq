// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

 using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Transactions;
using NUnit.Framework;

namespace Shaolinq.Tests
{
	[TestFixture("MySql")]
	[TestFixture("Sqlite")]
	[TestFixture("Postgres.DotConnect")]
	public class ConcurrencyTests
		: BaseTests
	{
		public ConcurrencyTests(string providerName)
			: base(providerName)
		{
			using (var scope = new TransactionScope())
			{
				var school = this.model.Schools.Create();

				school.Name = "Lee's Kung Fu School";

				scope.Complete();
			}
		}

		[Test]
		public void Test_Query_On_Lots_Of_Threads_No_TransactionScope()
		{
			var exceptions = new List<Exception>();
			var threads = new List<Thread>();
			var random = new Random();

			for (var i = 0; i < 10; i++)
			{
				var action = (ThreadStart)delegate
				{
					try
					{
						for (var j = 0; j < 50; j++)
						{
							Thread.Sleep(random.Next(0, 200));

							var user = this.model.Schools.First();
							Assert.IsNotNull(user);
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

			Assert.AreEqual(0, exceptions.Count);

			exceptions.ForEach(Console.WriteLine);
		}
	}
}
