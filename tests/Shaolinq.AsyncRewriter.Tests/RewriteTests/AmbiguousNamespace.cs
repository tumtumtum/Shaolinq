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

			/*
			 This *should* output:
			 
			 Other.Foo.Bar
			 Something.Other.Foo.Bar
			 
			 But currently AsyncRewriter messes up the namespaces and it outputs:

			 Something.Other.Foo.Bar
			 Something.Other.Foo.Bar
			 
			 If you change the commented lines in the methods below, it instead won't compile due to the method 'OtherBar' not being found in 'TestOtherFoo()'
			*/

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