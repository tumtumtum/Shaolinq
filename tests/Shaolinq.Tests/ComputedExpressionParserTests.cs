// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.IO;
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

			public long Foo()
			{
				return 101;
			}
		}

		[Test]
		public void TestParse()
		{
			var parser = new ComputedExpressionParser(new StringReader("C.Foo()"), typeof(TestObject).GetProperty("A"));

			var func = parser.Parse().Compile();

			var obj = new TestObject
			{
				B = 10,
				C = new TestObject {B = 20}
			};

			Console.WriteLine(((Func<TestObject, int>)func)(obj));
		}
	}
}
