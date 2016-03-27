using System;
using System.IO;
using System.Linq;
using NUnit.Framework;

namespace Shaolinq.Rewriter
{
	[TestFixture]
	public class Tests
	{
		[Test]
		public void Test()
		{
			var paths = Directory.EnumerateFiles(@"..\..\..\..\src\Shaolinq", "*.cs", SearchOption.AllDirectories)
				.Where(c => !Path.GetFileName(c).StartsWith("Generated")).ToArray();

			var s = ExpressionComparerWriter.Write(paths);

			Console.WriteLine(s);
		}
	}
}
