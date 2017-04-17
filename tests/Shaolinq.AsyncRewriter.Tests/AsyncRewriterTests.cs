// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;

namespace Shaolinq.AsyncRewriter.Tests
{
	[TestFixture(Category = "IgnoreOnMono")]
	public class AsyncRewriterTests
	{
		[Test]
		public void TestRewrite()
		{
			var rewriter = new Rewriter();
			var root = Path.GetDirectoryName(new Uri(this.GetType().Assembly.CodeBase).LocalPath);
			var paths = new List<string> { "Foo.cs", "Bar.cs" };
			
			var result = rewriter.RewriteAndMerge(paths.Select(c => Path.Combine(root, c)).ToArray());

			Console.WriteLine(result);
		}

		[Test]
		public void TestWithGenericConstraints()
		{
			var rewriter = new Rewriter();
			var root = Path.GetDirectoryName(new Uri(this.GetType().Assembly.CodeBase).LocalPath);
			var paths = new List<string> { "IQuery.cs" };

			var result = rewriter.RewriteAndMerge(paths.Select(c => Path.Combine(root, c)).ToArray());

			Console.WriteLine(result);
		}

		[Test]
		public void TestWithInterfaces()
		{
			var rewriter = new Rewriter();
			var root = Path.GetDirectoryName(new Uri(this.GetType().Assembly.CodeBase).LocalPath);
			var paths = new List<string> { "ICommand.cs" };

			var result = rewriter.RewriteAndMerge(paths.Select(c => Path.Combine(root, c)).ToArray());

			Console.WriteLine(result);
		}

		[Test]
		public void TestUsingExtensionMethods()
		{
			var rewriter = new Rewriter();
			var root = Path.GetDirectoryName(new Uri(this.GetType().Assembly.CodeBase).LocalPath);
			var paths = new List<string> { "ExtensionMethodTests.cs", "Foo.cs" };

			var result = rewriter.RewriteAndMerge(paths.Select(c => Path.Combine(root, c)).ToArray());

			Console.WriteLine(result);
		}

		[Test]
		public void TestConditionalAccess()
		{
			var rewriter = new Rewriter();
			var root = Path.GetDirectoryName(new Uri(this.GetType().Assembly.CodeBase).LocalPath);
			var paths = new List<string> { "ConditionalAccess.cs" };

			var result = rewriter.RewriteAndMerge(paths.Select(c => Path.Combine(root, c)).ToArray(), new[] { typeof(EnumerableExtensions).Assembly.Location }).ToArray();

			Console.WriteLine(result);
		}

		[Test]
		public void TestAssignment()
		{
			var rewriter = new Rewriter();
			var root = Path.GetDirectoryName(new Uri(this.GetType().Assembly.CodeBase).LocalPath);
			var paths = new List<string> { "TestAssignment.cs" };

			var result = rewriter.RewriteAndMerge(paths.Select(c => Path.Combine(root, c)).ToArray());

			Console.WriteLine(result);
		}

		[Test]
		public void TestTestGenericSpecialisedImplementation()
		{
			var rewriter = new Rewriter();
			var root = Path.GetDirectoryName(new Uri(this.GetType().Assembly.CodeBase).LocalPath);
			var paths = new List<string> { "TestGenericSpecialisedImplementation.cs" };

			var result = rewriter.RewriteAndMerge(paths.Select(c => Path.Combine(root, c)).ToArray());

			Console.WriteLine(result);
		}

		[Test]
		public void TestTestExplicitAwaitRewrittenAsyncMethod()
		{
			var rewriter = new Rewriter();
			var root = Path.GetDirectoryName(new Uri(this.GetType().Assembly.CodeBase).LocalPath);
			var paths = new List<string> { "TestExplicitAwaitRewrittenAsyncMethod.cs" };

			var result = rewriter.RewriteAndMerge(paths.Select(c => Path.Combine(root, c)).ToArray());
			if (result == null)
			{
				throw new ArgumentNullException(nameof(result));
			}

			Console.WriteLine(result);
		}

		[Test]
		public void TestExplicitInterfaceImplementationsMethod()
		{
			var rewriter = new Rewriter();
			var root = Path.GetDirectoryName(new Uri(this.GetType().Assembly.CodeBase).LocalPath);
			var paths = new List<string> { "TestExplicitInterfaceImplementations.cs" };

			var result = rewriter.RewriteAndMerge(paths.Select(c => Path.Combine(root, c)).ToArray());

			Console.WriteLine(result);
		}
	}
}
