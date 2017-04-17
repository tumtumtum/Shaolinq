using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shaolinq.AsyncRewriter.Tests
{
	public partial interface IFoo
	{
		[RewriteAsync]
		void Test1();
	}

	public partial class TestExplicitInterfaceImplementations
		: IFoo
	{
		[RewriteAsync]
		public void Test()
		{
			((IFoo)this).Test1();
		}

		[RewriteAsync]
		void IFoo.Test1()
		{
		}
	}
}
