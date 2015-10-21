// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Shaolinq.Persistence;
using Shaolinq.Persistence.Linq;
﻿using Shaolinq.Persistence.Linq.Expressions;

namespace Shaolinq
{
	public class ReusableQueryable<T>
		: IOrderedQueryable<T>
	{
		public ReusableQueryable()
		{
		}

		public ReusableQueryable(IPersistenceQueryProvider provider)
		{
			this.PersistenceQueryProvider = provider;
			this.Expression = Expression.Constant(this);
		}

		public ReusableQueryable(IPersistenceQueryProvider provider, Expression expression)
		{
            if (!typeof(IQueryable<T>).IsAssignableFrom(expression.Type) && !(expression is SqlProjectionExpression))
			{
				throw new ArgumentOutOfRangeException(nameof(expression));
            }
            
			this.PersistenceQueryProvider = provider;
			this.Expression = expression;
        }

		protected virtual void Initialize(IPersistenceQueryProvider provider, Expression expression)
		{
			this.PersistenceQueryProvider = provider;
			this.Expression = expression ?? Expression.Constant(this);
		}

		public virtual IEnumerator<T> GetEnumerator()
		{
			return this.PersistenceQueryProvider.GetEnumerable<T>(this.Expression).GetEnumerator();
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return this.GetEnumerator();
		}

		public virtual Type ElementType => typeof(T);

		public virtual Expression Expression
		{
			get;
			private set;
		}

		public virtual IQueryProvider Provider => this.PersistenceQueryProvider;

		public virtual IPersistenceQueryProvider PersistenceQueryProvider
		{
			get;
			private set;
		}

		public virtual string OriginalToString()
		{
			return base.ToString();
		}

		public override string ToString()
		{
			return ((SqlQueryProvider)this.Provider).GetQueryText(this.Expression);
		}
	}
}
