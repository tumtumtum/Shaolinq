using Other;
using Shaolinq.AsyncRewriter.Tests;

namespace Something.AsyncRewriter.Tests.RewriteTests
{
	public partial class AmbiguousNamespace
	{
		[RewriteAsync]
		public static int Main()
		{
			var otherFoo = new Foo();
			var somethingOtherFoo = new Something.Other.Foo();

			TestOtherFoo(otherFoo);
			TestSomethingOtherFoo(somethingOtherFoo);

			return 0;
		}

		[RewriteAsync]
		public static void TestOtherFoo(Foo foo) // Other.Foo
		{
			foo.Bar();
			//foo.OtherBar();
		}

		[RewriteAsync]
		public static void TestSomethingOtherFoo(Other.Foo foo) // Something.Other.Foo
		{
			foo.Bar();
			//foo.SomethingOtherBar();
		} 
	}
}