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
	/// Base class that represents a queryable set of <c>DataAccessObjects</c>
	/// </summary>
	/// <typeparam name="T">The type data access object</typeparam>
	public class DataAccessObjectsQueryable<T>
		: ReusableQueryable<T>, IHasDataAccessModel, IDataAccessObjectActivator
		where T : class, IDataAccessObject
	{
		private static class PropertyInfoCache<U>
		{
			public static readonly PropertyInfo IdPropertyInfo = typeof(U).GetProperties().FirstOrDefault(c => c.Name == "Id");
			public static readonly MethodInfo EnumerableContainsMethod = MethodInfoFastRef.EnumerableContainsMethod.MakeGenericMethod(typeof(U));
		}

		private readonly TypeDescriptor typeDescriptor;
		public DataAccessModel DataAccessModel { get; set; }
		public LambdaExpression ExtraCondition { get; protected set; }

		protected DataAccessObjectsQueryable(DataAccessModel dataAccessModel, Expression expression)
		{
			if (this.DataAccessModel != null)
			{
				throw new ObjectAlreadyInitializedException(this);
			}

			this.DataAccessModel = dataAccessModel; 
			this.typeDescriptor = dataAccessModel.TypeDescriptorProvider.GetTypeDescriptor(typeof(T));
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

		public virtual U Foo<U, K>(K primaryKey)
			where U : DataAccessObject<K>, T
		{
			return null;
		}

		public virtual T GetObject(object primaryKey, PrimaryKeyType primaryKeyType = PrimaryKeyType.Auto)
		{
			return GetQueryableByPrimaryKey(primaryKey, primaryKeyType).Single();
		}

		public virtual T GetObjectOrDefault(object primaryKey, PrimaryKeyType primaryKeyType = PrimaryKeyType.Auto)
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
				var deflated = this.DataAccessModel.GetReference<T>(primaryKey, primaryKeyType);
				var parameterExpression = Expression.Parameter(typeof(T), "value");
				var body = Expression.Equal(parameterExpression, Expression.Constant(deflated));

				condition = Expression.Lambda<Func<T, bool>>(body, parameterExpression);
			}

			return this.Where(condition);
		}

		public virtual IQueryable<T> GetObjects(params object[] primaryKeys)
		{
			return this.GetObjects(primaryKeys, PrimaryKeyType.Auto);
		}

		public virtual IQueryable<T> GetObjects(object[] primaryKeys, PrimaryKeyType primaryKeyType)
		{
			return this.GetObjects((IEnumerable<object>)primaryKeys, primaryKeyType);
		}

		public virtual IQueryable<T> GetObjects(IEnumerable<object> primaryKeys, PrimaryKeyType primaryKeyType = PrimaryKeyType.Auto)
		{
			if (primaryKeys == null)
			{
				throw new ArgumentNullException("primaryKeys");
			}

			List<T> converted = null;
			IList<object> primaryKeysCopy = null;
			var primaryKeysToWorkWith = primaryKeys as IList<object>;
		
			if (primaryKeysToWorkWith == null)
			{
				primaryKeysCopy = new List<object>();
				primaryKeysToWorkWith = primaryKeysCopy;
			}

			if (primaryKeyType != PrimaryKeyType.Composite)
			{
				converted = new List<T>();

				foreach (var obj in primaryKeys)
				{
					if (obj == null)
					{
						throw new ArgumentNullException("primaryKeys");
					}

					if (primaryKeysCopy != null)
					{
						primaryKeysCopy.Add(obj);
					}

					if (converted == null)
					{
						continue;
					}

					if (obj is T)
					{
						converted.Add((T)obj);
					}
					else if (TypeDescriptor.IsSimpleType(obj.GetType()))
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
				
				var deflatedObjects = primaryKeysToWorkWith
					.Select(c => this.DataAccessModel.GetReference<T>(c, primaryKeyType))
					.ToArray();

				var body = Expression.Call(null, PropertyInfoCache<T>.EnumerableContainsMethod, Expression.Constant(deflatedObjects), parameterExpression);

				condition = Expression.Lambda<Func<T, bool>>(body, parameterExpression);
			}

			return this.Where(condition);
		}
	}
}
