using System;
using Shaolinq.AsyncRewriter.Tests;

namespace Other
{
	public partial class Foo
	{
		[RewriteAsync]
		public void Bar()
		{
			Console.WriteLine("Other.Foo.Bar");
		}

		[RewriteAsync]
		public void OtherBar()
		{
			Console.WriteLine("Other.Foo.OtherBar");
		}
	}
}

namespace Something.Other
{
	public partial class Foo
	{
		[RewriteAsync]
		public void Bar()
		{
			Console.WriteLine("Something.Other.Foo.Bar");
		}

		[RewriteAsync]
		public void SomethingOtherBar()
		{
			Console.WriteLine("Something.Other.Foo.SomethingOtherBar");
		}
	}
}