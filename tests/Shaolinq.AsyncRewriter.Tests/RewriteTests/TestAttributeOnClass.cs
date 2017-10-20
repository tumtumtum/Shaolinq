using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shaolinq.AsyncRewriter.Tests
{
	public partial class TestAttributeOnClass: BaseClass
	{
		public void Test()
		{	
		}
	}

	[RewriteAsync(ApplyToDescendents = true)]
	public partial class BaseClass
	{
		public void Foo()
		{	
		}
	}
}
