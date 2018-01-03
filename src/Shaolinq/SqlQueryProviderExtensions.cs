// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Shaolinq.Persistence;

namespace Shaolinq
{
	internal static class SqlQueryProviderExtensions
	{
		public static Task<T> ExecuteAsync<T>(this IQueryProvider queryProvider, Expression expression, CancellationToken cancellationToken)
		{

			if (queryProvider is ISqlQueryProvider sqlQueryProvider)
			{
				return sqlQueryProvider.ExecuteAsync<T>(expression, cancellationToken);
			}
			else
			{
				return Task.FromResult(queryProvider.Execute<T>(expression));
			}
		}
	}
}