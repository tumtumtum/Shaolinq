using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Npgsql.Logging;
using NUnit.Framework;
using Shaolinq.Postgres;
using Shaolinq.Tests.TestModel;

namespace Shaolinq.Tests
{
	[TestFixture]
	public class LoadTests
	{
		[Test]
		public void StressTest()
		{
			NpgsqlLogManager.Provider = new ConsoleLoggingProvider(NpgsqlLogLevel.Info, true, true);

			var config = PostgresConfiguration.Create("StressTest", "localhost", "postgres", "postgres");
			(config.SqlDatabaseContextInfos[0] as PostgresSqlDatabaseContextInfo).MaxPoolSize = 2;
			(config.SqlDatabaseContextInfos[0] as PostgresSqlDatabaseContextInfo).KeepAlive = 0;

			var dataModel = DataAccessModel.BuildDataAccessModel<TestDataAccessModel>(config);

			dataModel.Create(DatabaseCreationOptions.DeleteExistingDatabase);

			Console.WriteLine(dataModel.GetCurrentSqlDatabaseContext().ConnectionString);

			Guid testStudentId;

			using (var scope = new DataAccessScope())
			{
				var school = dataModel.Schools.Create();
				var student = dataModel.Students.Create();

				student.School = school;
				student.Firstname = "Tum";
				student.Lastname = "Nguyen";

				testStudentId = student.Id;

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
							dataModel.Students.First(x => x.Firstname == "Student 100");
						}
						catch (Exception ex)
						{
							Console.WriteLine("Test error: {0}", ex);
						}
					}

					//Console.WriteLine("Stopped");
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
