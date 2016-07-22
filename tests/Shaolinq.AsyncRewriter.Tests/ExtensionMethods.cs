using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

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

	public static class QueryableExtensions
	{
		public static Task<bool> FirstOrDefaultAsync<T>(this IQueryable<T> source, Expression<Func<T, bool>> predicate)
		{
			return null;
		}
	}

	public class QQQ<T> : IQueryable<T>
	{
		public Type ElementType { get; }
		public Expression Expression { get; }
		public IQueryProvider Provider { get; }
		IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

		public QQQ(Expression expression, IQueryProvider provider, Type elementType)
		{
			this.Expression = expression;
			this.Provider = provider;
			this.ElementType = elementType;
		}

		public IEnumerator<T> GetEnumerator()
		{
			throw new NotImplementedException();
		}
	}

	public class ExtensionMethods
	{
		[RewriteAsync]
		public void Test()
		{
			var x = default(QQQ<string>);

			var z = x.FirstOrDefault(c => true);
			
			var y = new { x = default(List<Animal>) };

			var ey = new { enumerator = default(IAsyncEnumerator<string>) };

			ey.enumerator?.MoveNext();
			
			D.Foo(y.x, "");
			y.x.Foo("");
		}
	}
}
