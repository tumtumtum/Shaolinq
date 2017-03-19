// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.IO;
using System.Linq;
using NUnit.Framework;

namespace Shaolinq.ExpressionWriter
{
	[TestFixture]
	public class Tests
	{
		[Test]
		public void TestWriter()
		{
			var paths = Directory.EnumerateFiles(@"..\..\..\..\src\Shaolinq", "*.cs", SearchOption.AllDirectories)
				.Where(c => !Path.GetFileName(c).StartsWith("Generated")).ToArray();

			var s = ExpressionComparerWriter.Write(paths);

			Console.WriteLine(s);
		}

		[Test]
		public void TestHasher()
		{
			var paths = Directory.EnumerateFiles(@"..\..\..\..\src\Shaolinq", "*.cs", SearchOption.AllDirectories)
				.Where(c => !Path.GetFileName(c).StartsWith("Generated")).ToArray();

			var s = ExpressionHasherWriter.Write(paths);

			Console.WriteLine(s);
		}
	}
}
