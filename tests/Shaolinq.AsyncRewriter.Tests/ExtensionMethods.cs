using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Shaolinq;

namespace Shaolinq.AsyncRewriter.Tests
{
	public static class E
	{
		public static Task<int> FooAsync<T>(this IEnumerable<T> foo, string s)
		{
			return Task.FromResult(0);
		}
	}

	public class Animal
	{
	}

	public static class D
	{
		public static int Foo(this List<Animal> foo, string s)
		{
			return 0;
		}

		public static int Foo(this IEnumerable<Animal> foo, string s)
		{
			return 0;
		}

		public static Task<int> FooAsync(this List<Animal> foo, string s)
		{
			return null;
		}
	}

	public interface IAsyncEnumerator<T>: IEnumerator<T>
	{	
	}

	public static class EnumeratorExtensions
	{
		public static Task<bool> MoveNextAsync<T>(this IAsyncEnumerator<T> enumerable)
		{
			return null;	
		}
	}

	public class ExtensionMethods
	{
		[RewriteAsync]
		public async void Test()
		{
			var y = new { x = default(List<Animal>) };

			var enumerator= default(IAsyncEnumerator<string>);

			var ey = new { enumerator = default(IAsyncEnumerator<string>) };

			ey.enumerator?.MoveNext();
			
			D.Foo(y.x, "");
			y.x.Foo("");
		}
	}
}
