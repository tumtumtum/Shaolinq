// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

﻿using System;
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
		protected Type QueryableType { get; private set; }

		protected ReusableQueryProvider(Type queryableType)
		{
			this.QueryableType = queryableType;
		}

		public virtual IQueryable<T> CreateQuery<T>(Expression expression)
		{
			return (IQueryable<T>) CreateQuery(expression);
		}

		public virtual IQueryable CreateQuery(Expression expression)
		{
			var elementType = TypeHelper.GetElementType(expression.Type);

			try
			{
				return(IQueryable)Activator.CreateInstance(this.QueryableType.MakeGenericType(elementType), new object[] {this, expression});
			}
			catch (TargetInvocationException e)
			{
				throw e.InnerException;
			}
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

		public IRelatedDataAccessObjectContext RelatedDataAccessObjectContext
		{
			get;
			set;
		}
	}
}
