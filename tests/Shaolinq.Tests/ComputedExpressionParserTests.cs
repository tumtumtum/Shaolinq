using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Shaolinq.Parser;

namespace Shaolinq.Tests
{
	[TestFixture]
	public class ComputedExpressionParserTests
	{
		public class TestObject
		{
			public int A { get; set; }
			public int B { get; set; }

			public TestObject C;
		}

		[Test]
		public void TestParse()
		{
			var parser = new ComputedExpressionParser<TestObject, int>(new StringReader("C"), typeof(TestObject).GetProperty("A"));

			var func = parser.Parse().Compile();

			var obj = new TestObject
			{
				B = 10,
				C = new TestObject {B = 20}
			};

			Console.WriteLine(func(obj));
		}
	}
}
