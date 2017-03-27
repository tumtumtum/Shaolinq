using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Platform;

namespace Shaolinq.Tests
{
	[TestFixture]
	public class ReflectionPerformanceTests
	{
		private static readonly MethodInfo Test_MethodBaseMethod = TypeUtils.GetMethod<ReflectionPerformanceTests>(c => c.Test_MethodBase());

		[Test]
		public void Test_MethodBase()
		{
			const int iterations = 1000000;

			var stopwatch = new Stopwatch();

			stopwatch.Start();

			for (var i = 0; i < iterations; i++)
			{
				var name = MethodBase.GetCurrentMethod().Name;
			}

			stopwatch.Stop();

			Console.WriteLine($"MethodBase: {stopwatch.ElapsedMilliseconds}ms");

			stopwatch.Restart();

			for (var i = 0; i < iterations; i++)
			{
				var name = TypeUtils.GetMethod<ReflectionPerformanceTests>(c => c.Test_MethodBase()).Name;
			}

			stopwatch.Stop();

			Console.WriteLine($"TypeUtils.GetMethod: {stopwatch.ElapsedMilliseconds}ms");

			stopwatch.Restart();

			for (var i = 0; i < iterations; i++)
			{
				var name = Test_MethodBaseMethod.Name;
			}

			stopwatch.Stop();

			Console.WriteLine($"TypeUtils.GetMethodOnce: {stopwatch.ElapsedMilliseconds}ms");
		}
	}
}
