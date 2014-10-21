using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Shaolinq.Tests.TestModel;

namespace Shaolinq.Tests
{
	[TestFixture]
	public class FooTests
	{
		public class Fark<T>
		{
			public virtual T GetFoo<K>(K foo)
			{
				throw new InvalidOperationException();
			}
		}

		[Test]
		public void Test()
		{
			var students = new Fark<string>();

			var s = students.GetFoo(Guid.NewGuid());

			s = s;
		}
	}
}
