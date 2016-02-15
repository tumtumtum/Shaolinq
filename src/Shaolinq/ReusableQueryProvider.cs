// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Shaolinq.Persistence;

namespace Shaolinq
{
	public abstract class ReusableQueryProvider
		: ISqlQueryProvider
	{
		public virtual IQueryable<T> CreateQuery<T>(Expression expression)
		{
			return (IQueryable<T>)this.CreateQuery(expression);
		}
		
		public virtual IQueryable CreateQuery(Expression expression)
		{
			var elementType = TypeHelper.GetElementType(expression.Type);

			return this.CreateQuery(elementType, expression);
		}

		public abstract T Execute<T>(Expression expression);
        public abstract object Execute(Expression expression);
        public abstract Task<T> ExecuteAsync<T>(Expression expression);
        public abstract IEnumerable<T> GetEnumerable<T>(Expression expression);
        public abstract IAsyncEnumerable<T> GetAsyncEnumerable<T>(Expression expression);
        public abstract string GetQueryText(Expression expression);
		protected abstract IQueryable CreateQuery(Type elementType, Expression expression);
		public IRelatedDataAccessObjectContext RelatedDataAccessObjectContext { get; set; }
	}
}
