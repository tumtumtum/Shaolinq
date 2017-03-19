// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Shaolinq.Postgres;
using Shaolinq.Tests.TestModel;

namespace Shaolinq.Tests
{
	[TestFixture("Postgres")]
	public class LoadTests
		: BaseTests<TestDataAccessModel>
	{
		public LoadTests(string providerName)
			: base(providerName)
		{
			using (var scope = DataAccessScope.CreateReadCommitted())
			{
				this.model.Cats.Create();

				scope.Complete();
			}
		}
		
		[Test]
		public void Test_Lots_Of_Threads_Async()
		{
			var threadCount = 1000;
			var threads = new List<Thread>(threadCount);

			for (var i = 0; i < threadCount; i++)
			{
				var thread = new Thread(() => this.GetCatsNoDataAccessScopeAsync(i).Wait());

				thread.Start();
				threads.Add(thread);
			}

			threads.ForEach(x => x.Join());
		}

		public async Task<List<dynamic>> GetCategoriesAsync(int iteration)
		{
			using (var scope = DataAccessScope.CreateReadCommitted())
			{
				var result = await this.GetCatsNoDataAccessScopeAsync(iteration);

				await scope.CompleteAsync();

				return result;
			}
		}

		public async Task<List<dynamic>> GetCatsNoDataAccessScopeAsync(int iteration)
		{
			var cats = await this.model.Cats.Select(x =>
				new 
				{
					x.Id, x.Name
				})
				.AsEnumerable()
				.Cast<dynamic>()
				.ToListAsync();

			var tasks = new List<Task>();

			for (var i = 0; i < 1; i++)
			{
				tasks.Add(this.model.Cats.Skip(i).Take(1).FirstOrDefaultAsync());
			}

			await Task.WhenAll(tasks);

			return cats;
		}
		
		[Test, Ignore("Not yet")]
		public void StressTest()
		{
			var config = PostgresConfiguration.Create("StressTest", "localhost", "postgres", "postgres");
			(config.SqlDatabaseContextInfos[0] as PostgresSqlDatabaseContextInfo).MaxPoolSize = 2;
			(config.SqlDatabaseContextInfos[0] as PostgresSqlDatabaseContextInfo).KeepAlive = 0;

			var dataModel = DataAccessModel.BuildDataAccessModel<TestDataAccessModel>(config);

			dataModel.Create(DatabaseCreationOptions.DeleteExistingDatabase);

			Console.WriteLine(dataModel.GetCurrentSqlDatabaseContext().ConnectionString);

			using (var scope = new DataAccessScope())
			{
				var school = dataModel.Schools.Create();
				var student = dataModel.Students.Create();

				student.School = school;
				student.Firstname = "Tum";
				student.Lastname = "Nguyen";

				for (var i = 0; i < 10000; i++)
				{
					var s2 = dataModel.Students.Create();

					s2.School = school;
					s2.Firstname = "Student " + i;
				}

				scope.Complete();
			}

			const int numThreads = 10;

			var cancellationTokenSource = new CancellationTokenSource();

			var resetEvents = new List<WaitHandle>();

			for (var i = 0; i < numThreads; i++)
			{
				var resetEvent = new ManualResetEvent(false);
				resetEvents.Add(resetEvent);

				var dispatchThread = new Thread(_ =>
				{
					while (!cancellationTokenSource.Token.IsCancellationRequested)
					{
						try
						{
							dataModel.Students.ToList();
						}
						catch (Exception ex)
						{
							Console.WriteLine("Test error: {0}", ex);
						}
					}
					
					resetEvent.Set();
				})
				{ Name = $"Thread: {i + 1}" };

				dispatchThread.Start();
			}

			Thread.Sleep(10000);

			cancellationTokenSource.Cancel();

			WaitHandle.WaitAll(resetEvents.ToArray());
		}
	}
}
