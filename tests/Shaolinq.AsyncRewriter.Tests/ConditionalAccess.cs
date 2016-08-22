using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shaolinq.AsyncRewriter.Tests
{
	public class RewriteAsyncAttribute : Attribute
	{
	}

	public interface IFoo
	{
		[RewriteAsync]
		int Bar(string s);
	}
	
	public class ConditionalAccess<T>
	{
		public class Foo: IFoo
		{
			[RewriteAsync]
			public int Bar(string s)
			{
				return 0;
			}

			public Foo Other(int x)
			{
				return null;
			}
		}

		public class Car
		{
			public void Toot(bool value)
			{
			}
		}

		public Foo foo;

		private Foo GetCurrentFoo()
		{
			return null;
		}
		/*
		[RewriteAsync]
		public void Test()
		{
			this.foo?.Bar();
		}

		[RewriteAsync()]
		public void Test2()
		{
			var foo = GetCurrentFoo();

			foo?.Other(0).Bar();
		}

		[RewriteAsync()]
		public void Test2()
		{
			var foo = GetCurrentFoo();

			foo?.Other(foo.Bar()).Bar();
		}*/


		[RewriteAsync]
		public IQueryable<T> GetAll()
		{
			return null;
		}

		[RewriteAsync]
		public IQueryable<U> GetBalls<U>()
		{
			return null;
		}

		public async void Test3Async()
		{
			if (true)
			{
				var q = await this.GetAllAsync();

				await q.Where(c => true);

				var x = q.Where(c => true).ToList();

				if (true)
				{
					var q2 = await this.GetAllAsync();

					await q2.Where(c => true).ToListAsync;
				}
			}
		}
	}
}
