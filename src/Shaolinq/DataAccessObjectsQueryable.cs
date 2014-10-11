// Copyright (c) 2007-2014 Thong Nguyen (tumtumtum@gmail.com)

 using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Shaolinq
{
	/// <summary>
	/// Base class that represents a queryable set of data access objects (maps directly to a queryable table).
	/// </summary>
	/// <typeparam name="T">The type data access object</typeparam>
	public class DataAccessObjectsQueryable<T>
		: ReusableQueryable<T>, IHasDataAccessModel, IDataAccessObjectActivator
		where T : class, IDataAccessObject
	{
		public DataAccessModel DataAccessModel { get; set; }
		public LambdaExpression ExtraCondition { get; protected set; }

		protected DataAccessObjectsQueryable(DataAccessModel dataAccessModel, Expression expression)
		{
			if (this.DataAccessModel != null)
			{
				throw new ObjectAlreadyInitializedException(this);
			}

			this.DataAccessModel = dataAccessModel;
			base.Initialize(this.DataAccessModel.NewQueryProvider(), expression);
		}

		public virtual T Create()
		{
			return this.DataAccessModel.CreateDataAccessObject<T>();
		}

		public virtual T Create(bool transient)
		{
			return this.DataAccessModel.CreateDataAccessObject<T>(transient);
		}

		IDataAccessObject IDataAccessObjectActivator.Create()
		{
			return this.Create();
		}

		public virtual T ReferenceTo(object primaryKey)
		{
			return this.DataAccessModel.GetReferenceByPrimaryKey<T>(primaryKey);
		}
        
		private static class PropertyInfoCache<TTT>
		{
			public static readonly MethodInfo EnumerableContainsMethod;
			public static readonly PropertyInfo IdPropertyInfo = typeof(TTT).GetProperties().FirstOrDefault(c => c.Name == "Id");
			
            static PropertyInfoCache()
			{
				foreach (var methodInfo in typeof(Enumerable).GetMethods())
				{
					if (methodInfo.IsGenericMethod
						&& methodInfo.Name == "Contains"
						&& methodInfo.GetParameters().Length == 2)
					{
						EnumerableContainsMethod = methodInfo.MakeGenericMethod(typeof(TTT));

						break;
					}
				}
			}
		}

		public virtual T GetByPrimaryKey<I>(I id)
		{
			var parameterExpression = Expression.Parameter(typeof(T), "value");

			var propertyInfo = PropertyInfoCache<T>.IdPropertyInfo;
			var body = Expression.Equal(Expression.Property(parameterExpression, propertyInfo), Expression.Constant(id));
			var condition = Expression.Lambda<Func<T, bool>>(body, parameterExpression);

			return this.Where(condition).First();
		}

		public virtual T GetByPrimaryKeyOrDefault<I>(I id)
		{
			var parameterExpression = Expression.Parameter(typeof(T), "value");

			var propertyInfo = PropertyInfoCache<T>.IdPropertyInfo;
			var body = Expression.Equal(Expression.Property(parameterExpression, propertyInfo), Expression.Constant(id));
			var condition = Expression.Lambda<Func<T, bool>>(body, parameterExpression);

			return this.Where(condition).FirstOrDefault();
		}

		public virtual IQueryable<T> GetByPrimaryKeys<I>(params I[] ids)
		{
			var parameterExpression = Expression.Parameter(typeof(T), "value");
            
			var propertyInfo = PropertyInfoCache<T>.IdPropertyInfo;
			var body = Expression.Call(null, PropertyInfoCache<I>.EnumerableContainsMethod, Expression.Constant(ids), Expression.Property(parameterExpression, propertyInfo));
			var condition = Expression.Lambda<Func<T, bool>>(body, parameterExpression);

			return this.Where(condition);
		}

		public virtual IQueryable<T> GetByPrimaryKeys<I>(IEnumerable<I> ids)
		{
			return GetByPrimaryKeys(ids.ToArray());
		}
	}
}
