using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Shaolinq.Persistence;

namespace Shaolinq
{
	/// <summary>
	/// Base class that represents a queryable set of data access objects (maps directly to a queryable table).
	/// </summary>
	/// <typeparam name="T">The type data access object</typeparam>
	public class DataAccessObjectsQueryable<T>
		: ReusableQueryable<T>, IHasDataAccessModel, IDataAccessObjectActivator
		where T : IDataAccessObject
	{
		/// <summary>
		/// A reference to the related <see cref="BaseDataAccessModel"/>.
		/// </summary>
		public BaseDataAccessModel DataAccessModel { get; set; }

		/// <summary>
		/// A reference to the <see cref="PersistenceContext"/> related to this object.
		/// </summary>
		public PersistenceContext PersistenceContext { get; set; }

		public LambdaExpression ExtraCondition { get; protected set; }

		/// <summary>
		/// Used to support the framework.  Do not call this method directly.
		/// </summary>
		public virtual void Initialize(BaseDataAccessModel dataAccessModel, Expression expression)
		{
			if (this.DataAccessModel != null || this.PersistenceContext != null)
			{
				throw new ObjectAlreadyInitializedException(this);
			}

			this.DataAccessModel = dataAccessModel;
			this.PersistenceContext = this.DataAccessModel.GetPersistenceContext(typeof(T));

			base.Initialize(this.PersistenceContext.NewQueryProvider(this.DataAccessModel, this.PersistenceContext), expression);
		}

		public virtual T NewDataAccessObject()
		{
			return this.DataAccessModel.NewDataAccessObject<T>();
		}

		public virtual T NewDataAccessObject(bool transient)
		{
			return this.DataAccessModel.NewDataAccessObject<T>(transient);
		}

		IDataAccessObject IDataAccessObjectActivator.NewDataAccessObject()
		{
			return this.NewDataAccessObject();
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