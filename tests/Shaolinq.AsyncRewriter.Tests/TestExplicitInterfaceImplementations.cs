using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shaolinq.AsyncRewriter.Tests
{
	public partial interface IFoo<T>
	{
		/// <summary>
		/// This is a test
		/// </summary>
		/// <remarks>
		/// This is a test remark
		/// </remarks>
		[RewriteAsync]
		void Test1(T a1);
	}

	/// <summary>
	/// This is a test
	/// </summary>
	/// <remarks>
	/// This is a test remark
	/// </remarks>
	public partial class TestExplicitInterfaceImplementations
		: IFoo<int>
	{
		/// <summary>
		/// This is a test
		/// </summary>
		/// <remarks>
		/// This is a test remark
		/// </remarks>
		[RewriteAsync]
		public void Test()
		{
			((IFoo<int>)this).Test1(0);
		}

		[RewriteAsync]
		void IFoo<int>.Test1(int a1)
		{
		}
	}
}
