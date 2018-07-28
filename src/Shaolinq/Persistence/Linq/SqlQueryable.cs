// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)

using System.Collections.Generic;
using System.Linq.Expressions;

namespace Shaolinq.Persistence.Linq
{
	public class SqlQueryable<T>
		: ReusableQueryable<T>
	{
		public SqlQueryable(SqlQueryProvider provider, Expression expression)
			: base(provider, expression)
		{
		}

		public override IEnumerator<T> GetEnumerator()
		{
			return this.SqlQueryProvider.GetEnumerable<T>(this.Expression).GetEnumerator();
		}
	}
}
