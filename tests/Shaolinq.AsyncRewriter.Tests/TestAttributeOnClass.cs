using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shaolinq.AsyncRewriter.Tests
{
	[AttributeUsage(AttributeTargets.Class)]
	public class RewriteAsyncAttribute : Attribute
	{
		public bool ApplyToDescendents { get; set; }
	}

	public partial class TestAttributeOnClass: BaseClass
	{
		public void Test()
		{	
		}
	}

	[RewriteAsync(ApplyToDescendents = true)]
	public class BaseClass
	{
		public void Foo()
		{	
		}
	}
}
