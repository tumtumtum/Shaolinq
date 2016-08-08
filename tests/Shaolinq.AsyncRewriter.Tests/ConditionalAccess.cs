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
			public void Bar()
			{	
			}
		}

		public class Car
		{
			public void Toot(bool value)
			{
			}
		}

		public Foo foo;

		[RewriteAsync]
		public void Test()
		{
			this.foo?.Bar();
		}
	}
}
