// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Shaolinq.Persistence
{
	public interface ISqlQueryProvider
		: IQueryProvider
	{
		IEnumerable<T> GetEnumerable<T>(Expression expression);
		IAsyncEnumerable<T> GetAsyncEnumerable<T>(Expression expression);
		IRelatedDataAccessObjectContext RelatedDataAccessObjectContext { get; set; }
		Task<T> ExecuteAsync<T>(Expression expression, CancellationToken cancellationToken);
        
        string GetQueryText(Expression expression);
	}
}
