using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shaolinq.AsyncRewriter.Tests
{
	public partial class TestExpressionBody
	{
		[RewriteAsync]
		public bool Test() => Foo();
		
		[RewriteAsync]
		public void Test2() => Bar();
		 
		[RewriteAsync]
		public bool Foo()
		{
			return true;	
		}

		[RewriteAsync]
		public void Bar()
		{
		}
	}
}
