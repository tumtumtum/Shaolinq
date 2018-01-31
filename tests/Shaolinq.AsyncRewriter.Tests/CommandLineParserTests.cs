using System.Linq;
using NUnit.Framework;

namespace Shaolinq.AsyncRewriter.Tests
{
	[TestFixture]
	public class CommandLineParserTests
	{
		[Test]
		public void Test1()
		{
			var array1 = CommandLineParser.ParseArguments(@"a b ""c "" "" files\hello.cs"" test.cs a.cs b.cs c ""d""");

			Assert.IsTrue(array1.SequenceEqual(new[] { "a", "b", "c ", " files\\hello.cs", "test.cs", "a.cs", "b.cs", "c", "d" }));
		}

		[Test]
		public void Test2()
		{
			var array1 = CommandLineParser.ParseArguments(@"a b ""c ""  "" files\hello.cs"" test.cs  a.cs b.cs c d");

			Assert.IsTrue(array1.SequenceEqual(new[] { "a", "b", "c ", " files\\hello.cs", "test.cs", "a.cs", "b.cs", "c", "d" }));
		}
	}
}
