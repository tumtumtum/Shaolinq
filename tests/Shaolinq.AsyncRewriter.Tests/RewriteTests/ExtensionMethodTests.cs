using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Shaolinq.AsyncRewriter.Tests
{
	public class Animal
	{
	}

	public static class EnumerableExtensions
	{
		public static Task<int> FooAsync<T>(this IEnumerable<T> foo, string s)
		{
			return Task.FromResult(0);
		}
	}

	public static class QueryableExtensions
	{
		public static Task<bool> FirstOrDefaultAsync<U>(this IQueryable<U> s, Expression<Func<U, bool>> predicate)
		{
			return null;
		}
	}

	public static class AnimalFarm
	{
		public static int Count(this List<Animal> foo, string s)
		{
			return 0;
		}

		public static int Count(this IEnumerable<Animal> foo, string s)
		{
			return 0;
		}

		public static Task<int> CountAsync(this List<Animal> foo, string s)
		{
			return null;
		}

		public static List<Animal> GetAnimals(List<Animal> a)
		{
			return new List<Animal>();
		}

		public static Task<List<Animal>> GetAnimalsAsync(List<Animal> a)
		{
			return null;
		}

		public static List<Animal> GetAnimals2()
		{
			return new List<Animal>();
		}

		public static Task<List<Animal>> GetAnimals2Async()
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

	public interface IQQQ<T, U, V> : IQueryable<T>
	{

	}

	public class QQQ<T> : IQQQ<T, string, string>
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

	public interface ICacheClient
	{
		void Set<T>(string collectionKey, string objKey, T obj, TimeSpan? expiry = null);
		Task SetAsync<T>(string collectionKey, string objKey, T obj, TimeSpan? expiry = null);
	}

	public partial class ExtensionMethodTests
	{
		public async void AsyncMethod()
		{
			ICacheClient client = null;

			await Task.Run(() => Console.WriteLine());

			client.Set<List<Guid>>("", "", new List<Guid>());
		}

		public void WhenAll<T>(IQueryable<T> q, Func<T, T> tasker)
		{
		}

		public Task WhenAllAsync<T>(IQueryable<T> q, Func<T, Task<T>> tasker)
		{
			return null;
		}

		private List<string> NewList()
		{
			return new List<string>();
		}

		private async Task<List<string>> NewListAsync()
		{
			await WhenAllTestAsync();

			return new List<string>();
		}

		[RewriteAsync]
		public void WhenAllTest()
		{
			var x = nameof(ExtensionMethodTests);

			WhenAll(default(IQueryable<List<string>>), c => NewList());
		}

		[RewriteAsync]
		public void TestCacheClient()
		{
			ICacheClient client = null;

			client.Set($"{Guid.NewGuid():N}", "", new List<Guid>());
		}

		[RewriteAsync]
		private List<Guid> NewIdList()
		{
			return new List<Guid>();
		}
		
		[RewriteAsync]
		public void Test<T>()
		{
			var providerList = Expression.Constant(NewIdList());

			AnimalFarm.Count(AnimalFarm.GetAnimals(AnimalFarm.GetAnimals2()), "");

			var w = default(IQueryable<Foo>);

			var predicate = default(Expression<Func<Foo, bool>>);

			w.Where(predicate);

			var x = default(IQQQ<int, string, string>);


			var z = x.FirstOrDefault(c => true);
			
			var y = new { x = default(List<Animal>) };

			var ey = new { enumerator = default(IAsyncEnumerator<string>) };

			ey.enumerator?.MoveNext();
			
			AnimalFarm.Count(y.x, "");
			y.x.Count("");
		}
	}
}
