using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Shaolinq.AsyncRewriter.Tests
{
	[TestFixture]
	public class ConditionalAccess
	{
		public class Foo
		{
			[RewriteAsync]
			public int Bar()
			{
			    return 0;
			}

			public Foo Other(int x)
			{
				return null;
			}
		}

		public class Car
		{
			public void Toot(bool value)
			{
			}
		}

		public Foo foo;

		private Foo GetCurrentFoo()
		{
			return null;
		}
		/*
		[RewriteAsync]
		public void Test()
		{
			this.foo?.Bar();
		}

		[RewriteAsync()]
		public void Test2()
		{
			var foo = GetCurrentFoo();

			foo?.Other(0).Bar();
		}*/

		[RewriteAsync()]
		public void Test2()
		{
			var foo = GetCurrentFoo();

			foo?.Other(foo.Bar()).Bar();
		}
	}
}
