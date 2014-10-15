// Copyright (c) 2007-2014 Thong Nguyen (tumtumtum@gmail.com)

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
		where T : class, IDataAccessObject
	{
		private readonly TypeDescriptor typeDescriptor;
		public DataAccessModel DataAccessModel { get; set; }
		public LambdaExpression ExtraCondition { get; protected set; }

		protected DataAccessObjectsQueryable(DataAccessModel dataAccessModel, Expression expression)
		{
			if (this.DataAccessModel != null)
			{
				throw new ObjectAlreadyInitializedException(this);
			}

			this.typeDescriptor = dataAccessModel.TypeDescriptorProvider.GetTypeDescriptor(typeof(T));

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

		public virtual T GetByPrimaryKey(object primaryKey, PrimaryKeyType primaryKeyType = PrimaryKeyType.Auto)
		{
			return GetQueryableByPrimaryKey(primaryKey, primaryKeyType).Single();
		}

		public virtual T GetByPrimaryKeyOrDefault(object primaryKey, PrimaryKeyType primaryKeyType = PrimaryKeyType.Auto)
		{
			return GetQueryableByPrimaryKey(primaryKey, primaryKeyType).SingleOrDefault();
		}

		private IQueryable<T> GetQueryableByPrimaryKey(object primaryKey, PrimaryKeyType primaryKeyType = PrimaryKeyType.Auto)
		{
			if (primaryKey == null)
			{
				throw new ArgumentNullException("primaryKey");
			}

			Expression<Func<T, bool>> condition;

			if (TypeDescriptor.IsSimpleType(primaryKey.GetType()) || primaryKeyType == PrimaryKeyType.Single)
			{
				if (this.typeDescriptor.PrimaryKeyCount != 1)
				{
					throw new ArgumentException("Composite primary key expected", "primaryKey");
				}
				
				var parameterExpression = Expression.Parameter(typeof(T), "value");
				var body = Expression.Equal(Expression.Property(parameterExpression, PropertyInfoCache<T>.IdPropertyInfo), Expression.Constant(Convert.ChangeType(primaryKey, typeof(T))));

				condition = Expression.Lambda<Func<T, bool>>(body, parameterExpression);
			}
			else
			{
				var deflated = this.DataAccessModel.GetReferenceByPrimaryKey<T>(primaryKey, primaryKeyType);
				var parameterExpression = Expression.Parameter(typeof(T), "value");
				var body = Expression.Equal(parameterExpression, Expression.Constant(deflated));

				condition = Expression.Lambda<Func<T, bool>>(body, parameterExpression);
			}

			return this.Where(condition);
		}

		public virtual IQueryable<T> GetObjectsByPrimaryKeys(params object[] primaryKeys)
		{
			return GetObjectsByPrimaryKeys(primaryKeys, PrimaryKeyType.Auto);
		}

		public virtual IQueryable<T> GetObjectsByPrimaryKeys(object[] primaryKeys, PrimaryKeyType primaryKeyType)
		{
			return GetObjectsByPrimaryKeys((IEnumerable<object>)primaryKeys, primaryKeyType);
		}

		public virtual IQueryable<T> GetObjectsByPrimaryKeys(IEnumerable<object> primaryKeys, PrimaryKeyType primaryKeyType = PrimaryKeyType.Auto)
		{
			List<T> converted = null;

			if (primaryKeys == null)
			{
				throw new ArgumentNullException("primaryKeys");
			} 
			
			var primaryKeysCopy = new List<object>();

			if (primaryKeyType != PrimaryKeyType.Composite)
			{
				converted = new List<T>();

				foreach (var obj in primaryKeys)
				{
					if (obj == null)
					{
						throw new ArgumentNullException("primaryKeys");
					}

					primaryKeysCopy.Add(obj);

					if (converted == null)
					{
						continue;
					}

					if (TypeDescriptor.IsSimpleType(obj.GetType()) || obj is IDataAccessObject)
					{
						converted.Add((T)Convert.ChangeType(obj, typeof(T)));
					}
					else
					{
						if (primaryKeyType == PrimaryKeyType.Single)
						{
							throw new ArgumentException("Element in primaryKey is not convertible to type " + typeof(T), "primaryKeys");
						}

						converted = null;
					}
				}
			}

			Expression<Func<T, bool>> condition;

			if (converted != null)
			{
				var propertyInfo = PropertyInfoCache<T>.IdPropertyInfo;
				var parameterExpression = Expression.Parameter(propertyInfo.PropertyType, "value");

				var body = Expression.Call(null, MethodInfoFastRef.EnumerableContainsMethod.MakeGenericMethod(propertyInfo.PropertyType), Expression.Constant(converted), Expression.Property(parameterExpression, propertyInfo));

				condition = Expression.Lambda<Func<T, bool>>(body, parameterExpression);
			}
			else
			{
				var parameterExpression = Expression.Parameter(typeof(T), "value");
				
				var deflatedObjects = primaryKeysCopy
					.Select(c => this.DataAccessModel.GetReferenceByPrimaryKey<T>(c, primaryKeyType))
					.ToArray();

				var body = Expression.Call(null, PropertyInfoCache<T>.EnumerableContainsMethod, Expression.Constant(deflatedObjects), parameterExpression);

				condition = Expression.Lambda<Func<T, bool>>(body, parameterExpression);
			}

			return this.Where(condition);
		}
	}
}
