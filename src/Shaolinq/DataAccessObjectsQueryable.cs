// Copyright (c) 2007-2014 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Shaolinq.Persistence;

namespace Shaolinq
{
	public interface IHasExtraCondition
	{
		LambdaExpression ExtraCondition { get; }
	}

	/// <summary>
	/// Base class that represents a queryable set of <c>DataAccessObjects</c>
	/// </summary>
	/// <typeparam name="T">The type data access object</typeparam>
	public class DataAccessObjectsQueryable<T>
		: ReusableQueryable<T>, IHasDataAccessModel, IHasExtraCondition, IDataAccessObjectActivator<T>
		where T : DataAccessObject
	{
		private readonly TypeDescriptor typeDescriptor;
		public DataAccessModel DataAccessModel { get; set; }
		public LambdaExpression ExtraCondition { get; protected set; }
		private static readonly Type IdType = PrimaryKeyInfoCache<T>.IdPropertyInfo.PropertyType;

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

		public virtual T Create<K>(K primaryKey)
		{
			return this.Create(primaryKey, PrimaryKeyType.Auto);
		}

		public virtual T Create<K>(K primaryKey, PrimaryKeyType primaryKeyType)
		{
			return this.DataAccessModel.CreateDataAccessObject<T, K>(primaryKey, primaryKeyType);
		}

		IDataAccessObjectAdvanced IDataAccessObjectActivator.Create()
		{
			return this.Create();
		}

		IDataAccessObjectAdvanced IDataAccessObjectActivator.Create<K>(K primaryKey)
		{
			return this.Create<K>(primaryKey);
		}

		public virtual T GetByPrimaryKey<K>(K primaryKey)
		{
			return this.GetByPrimaryKey(primaryKey, PrimaryKeyType.Auto);
		}

		public virtual T GetByPrimaryKey<K>(K primaryKey, PrimaryKeyType primaryKeyType)
		{
			return GetQueryableByPrimaryKey(primaryKey, primaryKeyType).Single();
		}

		public virtual T GetByPrimaryKeyOrDefault<K>(K primaryKey)
		{
			return GetByPrimaryKeyOrDefault(primaryKey, PrimaryKeyType.Auto);
		}

		public virtual T GetByPrimaryKeyOrDefault<K>(K primaryKey, PrimaryKeyType primaryKeyType)
		{
			return GetQueryableByPrimaryKey(primaryKey, primaryKeyType).SingleOrDefault();
		}

		public virtual IQueryable<T> GetQueryableByPrimaryKey<K>(K primaryKey, PrimaryKeyType primaryKeyType = PrimaryKeyType.Auto)
		{
			if (typeof(K) == IdType)
			{
				if (this.typeDescriptor.PrimaryKeyCount != 1)
				{
					throw new ArgumentException("Composite primary key expected", "primaryKey");
				}

				var parameterExpression = Expression.Parameter(typeof(T), "value");
				var body = Expression.Equal(Expression.Property(parameterExpression, PrimaryKeyInfoCache<T>.IdPropertyInfo), Expression.Constant(primaryKey));

				var condition = Expression.Lambda<Func<T, bool>>(body, parameterExpression);

				return this.Where(condition);
			}
			else if (primaryKeyType == PrimaryKeyType.Single ||	TypeDescriptor.IsSimpleType(typeof(K)))
			{
				if (this.typeDescriptor.PrimaryKeyCount != 1)
				{
					throw new ArgumentException("Composite primary key expected", "primaryKey");
				}

				var parameterExpression = Expression.Parameter(typeof(T), "value");
				var body = Expression.Equal(Expression.Property(parameterExpression, PrimaryKeyInfoCache<T>.IdPropertyInfo), Expression.Constant(Convert.ChangeType(primaryKey, IdType)));

				var condition = Expression.Lambda<Func<T, bool>>(body, parameterExpression);

				return this.Where(condition);
			}
			else
			{
				var deflated = this.DataAccessModel.GetReference<T, K>(primaryKey, primaryKeyType);
				var parameterExpression = Expression.Parameter(typeof(T), "value");
				var body = Expression.Equal(parameterExpression, Expression.Constant(deflated));

				var condition = Expression.Lambda<Func<T, bool>>(body, parameterExpression);

				return this.Where(condition);
			}
		}

		public virtual IQueryable<T> GetManyByPrimaryKey<K>(params K[] primaryKeys)
		{
			return this.GetManyByPrimaryKey(primaryKeys, PrimaryKeyType.Auto);
		}

		public virtual IQueryable<T> GetManyByPrimaryKey<K>(K[] primaryKeys, PrimaryKeyType primaryKeyType)
		{
			return this.GetManyByPrimaryKey((IEnumerable<K>)primaryKeys, primaryKeyType);
		}

		public virtual IQueryable<T> GetManyByPrimaryKey<K>(IEnumerable<K> primaryKeys)
		{
			return GetManyByPrimaryKey<K>(primaryKeys, PrimaryKeyType.Auto);
		}

		public virtual IQueryable<T> GetManyByPrimaryKey<K>(IEnumerable<K> primaryKeys, PrimaryKeyType primaryKeyType)
		{
			if (primaryKeys == null)
			{
				throw new ArgumentNullException("primaryKeys");
			}

			if (primaryKeyType == PrimaryKeyType.Single || TypeDescriptor.IsSimpleType(typeof(K)) || (typeof(K) == IdType && primaryKeyType != PrimaryKeyType.Composite))
			{
				if (!TypeDescriptor.IsSimpleType(IdType))
				{
					throw new ArgumentException(string.Format("Type {0} needs to be convertable to {1}", typeof(K), IdType), "primaryKeys");
				}

				if (this.typeDescriptor.PrimaryKeyCount != 1)
				{
					throw new ArgumentException("Composite primary key type expected", "primaryKeys");
				}
			}

			if (primaryKeyType == PrimaryKeyType.Single || TypeDescriptor.IsSimpleType(typeof(K)) || (typeof(K) == IdType && primaryKeyType != PrimaryKeyType.Composite))
			{
				var propertyInfo = PrimaryKeyInfoCache<T>.IdPropertyInfo;
				var parameterExpression = Expression.Parameter(propertyInfo.PropertyType, "value");

				var body = Expression.Call(null, MethodInfoFastRef.EnumerableContainsMethod.MakeGenericMethod(propertyInfo.PropertyType), Expression.Constant(primaryKeyType), Expression.Property(parameterExpression, propertyInfo));
				var condition = Expression.Lambda<Func<T, bool>>(body, parameterExpression);

				return this.Where(condition);
			}
			else
			{ 
				var parameterExpression = Expression.Parameter(typeof(T), "value");
				
				var deflatedObjects = primaryKeys
					.Select(c => this.DataAccessModel.GetReference<T, K>(c, primaryKeyType))
					.ToArray();

				var body = Expression.Call(null, PrimaryKeyInfoCache<T>.EnumerableContainsMethod, Expression.Constant(deflatedObjects), parameterExpression);
				var condition = Expression.Lambda<Func<T, bool>>(body, parameterExpression);

				return this.Where(condition);
			}
		}
	}
}
