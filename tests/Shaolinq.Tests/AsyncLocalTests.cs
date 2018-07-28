// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)

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

		[Test]
		public async Task Test()
		{
			var x = 0;
			this.model.AsyncLocalExecutionVersion = 10;

			ExecutionContext.SuppressFlow();

			await Task.Run(async () =>
			{
				x = this.model.AsyncLocalExecutionVersion;
				this.model.AsyncLocalExecutionVersion = 11;

				await Task.Yield();
			}).ConfigureAwait(false);

			Console.WriteLine(this.model.AsyncLocalExecutionVersion);

			Assert.AreNotEqual(10, x);
		}
	}
}
