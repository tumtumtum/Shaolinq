// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Shaolinq.Persistence;

namespace Shaolinq
{
	internal static class SqlQueryProviderExtensions
	{
		public static T ExecuteEx<T>(this IQueryProvider queryProvider, Expression expression)
		{
			return queryProvider.Execute<T>(expression);
		}

		public static Task<T> ExecuteExAsync<T>(this IQueryProvider queryProvider, Expression expression, CancellationToken cancellationToken)
		{
			var sqlQueryProvider = queryProvider as ISqlQueryProvider;

			if (sqlQueryProvider != null)
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