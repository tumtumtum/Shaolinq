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

			var result = rewriter.RewriteAndMerge(paths.Select(c => Path.Combine(root, c)).ToArray());

			Console.WriteLine(result);
		}
	}
}
