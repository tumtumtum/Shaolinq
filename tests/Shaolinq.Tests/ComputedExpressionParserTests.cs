// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.IO;
using System.Linq.Expressions;
using NUnit.Framework;
using Shaolinq.Persistence.Computed;

namespace Shaolinq.Tests
{
	[TestFixture]
	public class ComputedExpressionParserTests
	{
		public static class StaticUtils
		{
			public static int Add(int x, int y)
			{
				return x + y;
			}

			public static int Add(int x, int y, int z)
			{
				return x + y + z;
			}
		}

		public static int Bar()
		{
			return 3;
		}

		public class TestObject
		{
			public class TestStaticUtils
			{
				public static int Subtract(int x, int y)
				{
					return x - y;
				}
			}

			public int A { get; set; }
			public int B { get; set; }

			public TestObject C;

			public long Foo()
			{
				return 101;
			}

			public static string Make<T>(object x, T value)
			{
				return value.ToString();
			}
		}

		[Test]
		public void TestParse()
		{
			var property = typeof(TestObject).GetProperty("A");
			var func = ComputedExpressionParser.Parse(new StringReader("C.Foo()"), property, null, property.PropertyType).Compile();

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
			var property = typeof(TestObject).GetProperty("A");
			var func = ComputedExpressionParser.Parse(new StringReader("A = value + 1000"), property, null, property.PropertyType).Compile();

			var result = func.DynamicInvoke(new TestObject());
		}

		[Test]
		public void TestParse3()
		{
			var property = typeof(TestObject).GetProperty("A");
			var func =
				ComputedExpressionParser.Parse
					(new StringReader("A = Shaolinq.Tests.ComputedExpressionParserTests.Bar() + 1"), property, new[] { typeof(TestObject) }, property.PropertyType).Compile();

			Assert.AreEqual(4, func.DynamicInvoke(new TestObject()));
		}

		[Test]
		public void TestParse4a()
		{
			var property = typeof(TestObject).GetProperty("A");
			var func = ComputedExpressionParser.Parse(new StringReader("A = Shaolinq.Tests.ComputedExpressionParserTests.TestObject.Make(this, 1).GetHashCode()"), property, null, property.PropertyType).Compile();

			Assert.AreEqual("1".GetHashCode(), func.DynamicInvoke(new TestObject()));
		}

		[Test]
		public void TestParse4b()
		{
			var property = typeof(TestObject).GetProperty("A");
			var func = ComputedExpressionParser.Parse(new StringReader("A = Make(this, 1).GetHashCode()"), property, null, property.PropertyType).Compile();

			Assert.AreEqual("1".GetHashCode(), func.DynamicInvoke(new TestObject()));
		}

		[Test]
		public void TestParse5()
		{
			var property = typeof(TestObject).GetProperty("A");
			var func = ComputedExpressionParser.Parse(new StringReader("Shaolinq.Tests.ComputedExpressionParserTests.StaticUtils.Add(B, 7)"), property, null, property.PropertyType).Compile();

			Assert.AreEqual(17, func.DynamicInvoke(new TestObject { B = 10 }));
		}

		[Test]
		public void TestParse6()
		{
			Assert.Throws<InvalidOperationException>
			(() =>
			{
				var property = typeof(TestObject).GetProperty("A");
				var func = ComputedExpressionParser.Parse(new StringReader("StaticUtils.Add(B, 7)"), property, null, property.PropertyType).Compile();

				Assert.AreEqual(17, func.DynamicInvoke(new TestObject { B = 10 }));
			});
		}

		[Test]
		public void TestParse7()
		{
			var property = typeof(TestObject).GetProperty("A");
			var func = ComputedExpressionParser.Parse(new StringReader("Shaolinq.Tests.ComputedExpressionParserTests.StaticUtils.Add(B, 7)"), property, new [] { typeof(StaticUtils) }, property.PropertyType).Compile();

			Assert.AreEqual(17, func.DynamicInvoke(new TestObject { B = 10 }));
		}

		[Test]
		public void TestParse8()
		{
			var property = typeof(TestObject).GetProperty("A");
			var func = ComputedExpressionParser.Parse(new StringReader("Add(B, 7)"), property, new [] { typeof(StaticUtils) }, property.PropertyType).Compile();

			Assert.AreEqual(17, func.DynamicInvoke(new TestObject { B = 10 }));
		}

		[Test]
		public void TestParse9()
		{
			var property = typeof(TestObject).GetProperty("A");
			var func = ComputedExpressionParser.Parse(new StringReader("TestStaticUtils.Subtract(B, 7)"), property, null, property.PropertyType).Compile();

			Assert.AreEqual(3, func.DynamicInvoke(new TestObject { B = 10 }));
		}

		[Test]
		public void TestParse10()
		{
			var property = typeof(TestObject).GetProperty("A");
			var func = ComputedExpressionParser.Parse(new StringReader("A == 1"), Expression.Parameter(typeof(TestObject)), null, null).Compile();

			Assert.AreEqual(false, func.DynamicInvoke(new TestObject { A = 10 }));
			Assert.AreEqual(true, func.DynamicInvoke(new TestObject { A = 1 }));
		}
	}
}
