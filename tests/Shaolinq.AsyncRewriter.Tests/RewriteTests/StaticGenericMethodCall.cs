using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shaolinq.AsyncRewriter.Tests.RewriteTests
{
	public enum ErrorCodes
	{
		TestError
	}

	public class ResponseStatus
	{
		public ResponseStatus(string message)
		{

		}
	}

	public class BaseResponse
	{
		public int Status { get; set; }
		public ResponseStatus ResponseStatus { get; set; }
	}

	public class CreateAccount
	{

	}

	public class CreateAccountResponse : BaseResponse
	{

	}

	public class BaseStaticGenericMethodCall
	{
		
	}

	public class StaticGenericMethodCall : BaseStaticGenericMethodCall
	{
		protected TResponse Error<TResponse>(ErrorCodes errorCode)
			where TResponse : BaseResponse, new()
		{
			return new TResponse
			{
				Status = 0,
				ResponseStatus = new ResponseStatus(errorCode.ToString())
			};
		}

		protected static T Foo<T>(T x)
		{
			return default(T);
		}

		protected static Task<T> FooAsync<T>(T x)
		{
			return null;
		}

		public async Task<CreateAccountResponse> Post(CreateAccount request)
		{
			if (Environment.TickCount % 2 == 0)
			{
				Foo<string>("");
			}

			return await Task.Run(() => new CreateAccountResponse());
		}
	}
}
