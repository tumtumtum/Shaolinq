// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Shaolinq.AsyncRewriter.Tests
{
	[TestFixture]
	public class LambdaTests
	{
		public static void Foo()
		{
		}

		public static Task FooAsync()
		{
			return default(Task);
		}

		public static void Act(Action action)
		{
		}

		public static Task ActAsync(Action action)
		{
			return default(Task);
		}

		[RewriteAsync]
		public void Act1()
		{
			Act(() => Foo());
		}

		[Test]
		public void Test()
		{
			var rewriter = new Rewriter();
			var root = Path.GetDirectoryName(new Uri(GetType().Assembly.CodeBase).LocalPath);
			var paths = new List<string> { "LambdaTests.cs" };

			var result = rewriter.RewriteAndMerge(paths.Select(c => Path.Combine(root, c)).ToArray());

			Console.WriteLine(result);
		}
	}
}
