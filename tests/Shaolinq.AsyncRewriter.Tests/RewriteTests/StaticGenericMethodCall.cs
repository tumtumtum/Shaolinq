using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shaolinq.AsyncRewriter.Tests.RewriteTests
{
	public class StaticGenericMethodCall : BaseStaticGenericMethodCall
	{
		protected static T Foo<T>(T x)
		{
			return default(T);
		}

		public async Task Bar()
		{
			Foo<string>("");

			return null;
		}
	}
}
