// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Shaolinq.Persistence
{
	public interface ISqlQueryProvider
		: IQueryProvider
	{
		IRelatedDataAccessObjectContext RelatedDataAccessObjectContext { get; set; }
		IEnumerable<T> GetEnumerable<T>(Expression expression);
        Task<T> ExecuteAsync<T>(Expression expression, CancellationToken cancellationToken);
        IAsyncEnumerable<T> GetAsyncEnumerable<T>(Expression expression);

        string GetQueryText(Expression expression);
	}
}
