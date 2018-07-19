using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Shaolinq.AsyncRewriter.Tests
{
	public static partial class TestClass
	{
		public class Cat
		{

		}

		[RewriteAsync]
		public static void Test(this Cat cat, string name)
		{
		}

		[RewriteAsync]
		public static void Foo()
		{
			new Cat().Test("hello");
			Test(new Cat(), "hello");
			Tests.TestClass.Test(new Cat(), "hello");
		}
	}
}
