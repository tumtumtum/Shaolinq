// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Shaolinq.Persistence;
using Shaolinq.Persistence.Linq;

namespace Shaolinq
{
	public class ReusableQueryable<T>
		: IOrderedQueryable<T>
	{
		public ReusableQueryable(ISqlQueryProvider provider)
			: this(provider, null)
		{
		}

		public ReusableQueryable(ISqlQueryProvider provider, Expression expression)
		{
			this.Expression = expression ?? Expression.Constant(this);
			this.SqlQueryProvider = provider;
        }

		public Type ElementType => typeof(T);
		public Expression Expression { get; }
		public IQueryProvider Provider => this.SqlQueryProvider;
		public ISqlQueryProvider SqlQueryProvider { get; }
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => this.GetEnumerator();
		public override string ToString() => ((SqlQueryProvider)this.Provider).GetQueryText(this.Expression);
		public virtual IEnumerator<T> GetEnumerator() => this.SqlQueryProvider.GetEnumerable<T>(this.Expression).GetEnumerator();
	}
}
