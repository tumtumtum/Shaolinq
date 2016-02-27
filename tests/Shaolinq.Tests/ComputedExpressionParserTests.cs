// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.IO;
using NUnit.Framework;
using Shaolinq.Persistence.Computed;

namespace Shaolinq.Tests
{
	[TestFixture]
	public class ComputedExpressionParserTests
	{
		public static int Bar()
		{
			return 3;
		}

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

		[Test]
		public void TestParse2()
		{
			var parser = new ComputedExpressionParser(new StringReader("A = value + 1000"), typeof(TestObject).GetProperty("A"));

			var result = parser.Parse();
		}

		[Test]
		public void TestParse3()
		{
			var parser = new ComputedExpressionParser(new StringReader("A = Shaolinq.Tests.ComputedExpressionParserTests.Bar() + 1"), typeof(TestObject).GetProperty("A"));

			var result = parser.Parse();

			Assert.AreEqual(4, result.Compile().DynamicInvoke(new TestObject()));
		}
	}
}
