using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shaolinq.AsyncRewriter.Tests.RewriteTests
{
	public interface IReferencesAccount
	{
	}

	public interface IValidator
	{
		bool ValidateCreate<T>(T transaction) where T : IReferencesAccount;
		Task<bool> ValidateCreateAsync<T>(T transaction) where T : IReferencesAccount;
	}

	public class Validator: IValidator
	{
		public bool ValidateCreate<T>(T transaction)
			where T : IReferencesAccount
		{
			throw new InvalidOperationException("Should be using ValidateCreateAsync");
		}

		public Task<bool> ValidateCreateAsync<T>(T transaction)
			where T : IReferencesAccount
		{
			return Task.FromResult(default(bool));
		}
	}

	public class Provider : IReferencesAccount
	{
	}
	
	public partial class Test
	{
		private IValidator validator = new Validator();

		[RewriteAsync]
		private void Foo()
		{
			var provider = new Provider();

			validator.ValidateCreate(provider);
		}

		public static void Main()
		{
			new Test().FooAsync().GetAwaiter().GetResult();
		}
	}
}
