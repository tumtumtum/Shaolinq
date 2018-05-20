using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Shaolinq.AsyncRewriter.Tests.RewriteTests
{
	public interface IBase
	{
		void Create<T>(T value);
		Task CreateAsync<T>(T value);
		Task CreateAsync<T>(T value, CancellationToken cancellationToken);
	}

	public class Car
	{
	}

	public interface IFoo: IBase
	{
		void Create(Car value);
		Task CreateAsync(Car value);
	}

	public class Foo : IFoo
	{
		public void Create<T>(T value)
		{
			throw new InvalidOperationException();
		}

		public Task CreateAsync<T>(T value)
		{
			throw new InvalidOperationException();
		}

		public Task CreateAsync<T>(T value, CancellationToken cancellationToken)
		{
			throw new InvalidOperationException();
		}

		public void Create(Car value)
		{
		}

		public Task CreateAsync(Car value)
		{
			return Task.FromResult<int>(0);
		}
	}

	public partial class MethodResolutionTest
	{
		private IFoo foo = new Foo();

		[RewriteAsync]
		public void Test()
		{
			foo.Create(new Car());
		}

		[RewriteAsync]
		public static int Main()
		{
			new MethodResolutionTest().Test();

			return 0;
		}
	}
}
