// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Shaolinq.Persistence;

namespace Shaolinq
{
	public abstract class ReusableQueryProvider
		: IPersistenceQueryProvider
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

		public virtual T Execute<T>(Expression expression)
		{
			if (typeof(IEnumerable).IsAssignableFrom(typeof(T)))
			{
				return (T)this.Execute(expression);
			}
			else
			{
				return ((IEnumerable<T>)this.Execute(expression)).First();
			}
		}

		public abstract object Execute(Expression expression);
		public abstract IEnumerable<T> GetEnumerable<T>(Expression expression);
		public abstract string GetQueryText(Expression expression);
		protected abstract IQueryable CreateQuery(Type elementType, Expression expression);
		public IRelatedDataAccessObjectContext RelatedDataAccessObjectContext { get; set; }
	}
}
