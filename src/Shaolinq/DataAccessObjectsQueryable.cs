// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Shaolinq.Persistence;
using Shaolinq.TypeBuilding;

namespace Shaolinq
{
	/// <summary>
	/// Base class that represents a queryable set of <c>DataAccessObjects</c>
	/// </summary>
	/// <typeparam name="T">The type data access object</typeparam>
	public class DataAccessObjectsQueryable<T>
		: ReusableQueryable<T>, IHasDataAccessModel, IHasExtraCondition, IDataAccessObjectActivator<T>
		where T : DataAccessObject
	{
		private readonly Type idType;
		private readonly PropertyInfo primaryKeyProperty;
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

			if (typeDescriptor.PrimaryKeyCount > 0)
			{
				this.idType = typeDescriptor.PrimaryKeyProperties[0].PropertyType;
				this.primaryKeyProperty = typeDescriptor.PrimaryKeyProperties[0].PropertyInfo;
			}

			this.DataAccessModel = dataAccessModel; 
			this.Initialize(this.DataAccessModel.NewQueryProvider(), expression);
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
			return this.Create(primaryKey);
		}

		public virtual T GetByPrimaryKey<K>(K primaryKey)
		{
			return this.GetByPrimaryKey(primaryKey, PrimaryKeyType.Auto);
		}

		public virtual T GetByPrimaryKey<K>(K primaryKey, PrimaryKeyType primaryKeyType)
		{
			return this.GetQueryableByPrimaryKey(primaryKey, primaryKeyType).Single();
		}

		public virtual T GetByPrimaryKeyOrDefault<K>(K primaryKey)
		{
			return this.GetByPrimaryKeyOrDefault(primaryKey, PrimaryKeyType.Auto);
		}

		public virtual T GetByPrimaryKeyOrDefault<K>(K primaryKey, PrimaryKeyType primaryKeyType)
		{
			return this.GetQueryableByPrimaryKey(primaryKey, primaryKeyType).SingleOrDefault();
		}

		public virtual IQueryable<T> GetQueryableByPrimaryKey<K>(K primaryKey, PrimaryKeyType primaryKeyType = PrimaryKeyType.Auto)
		{
			if (idType == null)
			{
				throw new NotSupportedException($"Type {this.typeDescriptor.PersistedName} does not define any primary keys");
			}

			if (typeof(K) == idType && !idType.IsDataAccessObjectType())
			{
				if (this.typeDescriptor.PrimaryKeyCount != 1)
				{
					throw new ArgumentException("Composite primary key expected", nameof(primaryKey));
				}

				var parameterExpression = Expression.Parameter(typeof(T), "value");
				var body = Expression.Equal(Expression.Property(parameterExpression, primaryKeyProperty), Expression.Constant(primaryKey));

				var condition = Expression.Lambda<Func<T, bool>>(body, parameterExpression);

				return this.Where(condition);
			}
			else if (primaryKeyType == PrimaryKeyType.Single ||	TypeDescriptor.IsSimpleType(typeof(K)))
			{
				if (this.typeDescriptor.PrimaryKeyCount != 1)
				{
					throw new ArgumentException("Composite primary key expected", nameof(primaryKey));
				}

				var parameterExpression = Expression.Parameter(typeof(T), "value");
				var body = Expression.Equal(Expression.Property(parameterExpression, primaryKeyProperty), Expression.Constant(Convert.ChangeType(primaryKey, idType)));

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
			return this.GetManyByPrimaryKey<K>(primaryKeys, PrimaryKeyType.Auto);
		}

		public virtual IQueryable<T> GetManyByPrimaryKey<K>(IEnumerable<K> primaryKeys, PrimaryKeyType primaryKeyType)
		{
			if (idType == null)
			{
				throw new NotSupportedException($"Type {this.typeDescriptor.PersistedName} does not define any primary keys");
			}

			if (primaryKeys == null)
			{
				throw new ArgumentNullException(nameof(primaryKeys));
			}

			if (primaryKeyType == PrimaryKeyType.Single || TypeDescriptor.IsSimpleType(typeof(K)) || (typeof(K) == idType && primaryKeyType != PrimaryKeyType.Composite))
			{
				if (!TypeDescriptor.IsSimpleType(idType))
				{
					throw new ArgumentException($"Type {typeof(K)} needs to be convertable to {idType}", nameof(primaryKeys));
				}

				if (this.typeDescriptor.PrimaryKeyCount != 1)
				{
					throw new ArgumentException("Composite primary key type expected", nameof(primaryKeys));
				}
			}

			if (primaryKeyType == PrimaryKeyType.Single || TypeDescriptor.IsSimpleType(typeof(K)) || (typeof(K) == idType && primaryKeyType != PrimaryKeyType.Composite))
			{
				var propertyInfo = primaryKeyProperty;
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
