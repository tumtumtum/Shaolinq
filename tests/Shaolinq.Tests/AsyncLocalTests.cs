using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Shaolinq.Tests.TestModel;

namespace Shaolinq.Tests
{
	[TestFixture("Sqlite")]
	public class AsyncLocalTests: BaseTests<TestDataAccessModel>
	{
		public AsyncLocalTests(string providerName)
			: base(providerName)
		{
		}

		[Test, Ignore("Not yet")]
		public async Task Test()
		{
			var x = 0;
			model.AsyncLocalExecutionVersion = 10;

			ExecutionContext.SuppressFlow();

			await Task.Run(async () =>
			{
				x = model.AsyncLocalExecutionVersion;
				model.AsyncLocalExecutionVersion = 11;

				await Task.Yield();
			}).ConfigureAwait(false);

			Console.WriteLine(model.AsyncLocalExecutionVersion);

			Assert.AreNotEqual(10, x);
		}
	}
}
