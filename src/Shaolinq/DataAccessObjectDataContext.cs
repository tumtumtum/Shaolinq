// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Platform;
using Shaolinq.Logging;
using Shaolinq.Persistence;

// ReSharper disable SuspiciousTypeConversion.Global

namespace Shaolinq
{
	/// <summary>
	/// Stores a cache of all objects that have been loaded or created within a context
	/// of a transaction.
	/// Code repetition and/or ugliness in this class is due to the need for this
	/// code to run FAST.
	/// </summary>
	public partial class DataAccessObjectDataContext
	{
		public static readonly ILog Logger = LogProvider.GetCurrentClassLogger();

		public DataAccessModel DataAccessModel { get; }
		public SqlDatabaseContext SqlDatabaseContext { get; }
		
		internal bool isCommiting;
		internal readonly Dictionary<RuntimeTypeHandle, IObjectsByIdCache> cachesByType = new Dictionary<RuntimeTypeHandle, IObjectsByIdCache>();
		
		protected internal struct TypeAndTransactionalCommandsContext
		{
			public Type Type { get; }
			public SqlTransactionalCommandsContext CommandsContext { get; }

			public TypeAndTransactionalCommandsContext(Type type, SqlTransactionalCommandsContext sqlTransactionalCommandsContext)
				: this()
			{
				this.Type = type;
				this.CommandsContext = sqlTransactionalCommandsContext;
			}
		}

		private static CompositePrimaryKey GetDataAccessObjectCompositeId(DataAccessObject dataAccessObject)
		{
			return new CompositePrimaryKey(dataAccessObject.GetAdvanced().GetPrimaryKeys());
		}
		
		public DataAccessObjectDataContext(DataAccessModel dataAccessModel, SqlDatabaseContext sqlDatabaseContext)
		{
			this.DataAccessModel = dataAccessModel;
			this.SqlDatabaseContext = sqlDatabaseContext;
		}

		public virtual void Deleted(IDataAccessObjectAdvanced value)
		{
			if (value.IsDeleted)
			{
				return;
			}

			if ((value.ObjectState & DataAccessObjectState.Untracked) == DataAccessObjectState.Untracked)
			{
				return;
			}

			var typeHandle = Type.GetTypeHandle(value);
			
			if (!this.cachesByType.TryGetValue(typeHandle, out var cache))
			{
				cache = CreateCacheForDataAccessObject(value, this);

				this.cachesByType[typeHandle] = cache;
			}

			cache.Deleted((DataAccessObject)value);
		}

		public virtual void ImportObject(DataAccessObject value)
		{
			if (value == null)
			{
				return;
			}

			ImportObject(new HashSet<DataAccessObject>(), value);
		}

		protected void ImportObject(HashSet<DataAccessObject> alreadyVisited, DataAccessObject value)
		{
			CacheObject(value, true);

			alreadyVisited.Add(value);

			foreach (var propertyInfoAndValue in value.GetAdvanced().GetAllProperties())
			{
				if (propertyInfoAndValue.Value is DataAccessObject propertyValue && !alreadyVisited.Contains(propertyValue))
				{
					alreadyVisited.Add(propertyValue);

					ImportObject(alreadyVisited, propertyValue);
				}
			}
		}

		public virtual DataAccessObject GetObject(Type type, ObjectPropertyValue[] primaryKeys)
		{
			return this.cachesByType.TryGetValue(type.TypeHandle, out var cache) ? cache.Get(primaryKeys) : null;
		}

		public virtual DataAccessObject GetObject(Type type, LambdaExpression predicate)
		{
			return this.cachesByType.TryGetValue(type.TypeHandle, out var cache) ? cache.Get(predicate) : null;
		}

		private static Dictionary<RuntimeTypeHandle, Func<DataAccessObjectDataContext, IObjectsByIdCache>> cacheConstructor = new Dictionary<RuntimeTypeHandle, Func<DataAccessObjectDataContext, IObjectsByIdCache>>();

		private static IObjectsByIdCache CreateCacheForDataAccessObject(IDataAccessObjectAdvanced dataAccessObject, DataAccessObjectDataContext context)
		{
			var typeHandle = Type.GetTypeHandle(dataAccessObject);

			if (!cacheConstructor.TryGetValue(typeHandle, out var func))
			{
				Delegate getId;
				Expression keyComparer;
				
				var type = dataAccessObject.GetType();
				var keyType = dataAccessObject.NumberOfPrimaryKeys > 1 ? typeof(CompositePrimaryKey) : dataAccessObject.KeyType;
				var cacheType = typeof(ObjectsByIdCache<>).MakeGenericType(keyType);
				var constructor = cacheType.GetConstructors().Single();

				var getIdFuncType = typeof(Func<,>).MakeGenericType(typeof(DataAccessObject), keyType);

				if (keyType != typeof(CompositePrimaryKey))
				{
					keyComparer = Expression.Constant(null, typeof(IEqualityComparer<>).MakeGenericType(dataAccessObject.KeyType ?? dataAccessObject.CompositeKeyTypes[0]));

					var param = Expression.Parameter(typeof(DataAccessObject));
					var lambda = Expression.Lambda(dataAccessObject.TypeDescriptor.GetSinglePrimaryKeyExpression(Expression.Convert(param, type)), param);

					getId = lambda.Compile();
				}
				else
				{
					keyComparer = Expression.Constant(CompositePrimaryKeyComparer.Default);
					getId = Delegate.CreateDelegate(getIdFuncType, TypeUtils.GetMethod(() => GetDataAccessObjectCompositeId(default(DataAccessObject))));
				}

				var contextParam = Expression.Parameter(typeof(DataAccessObjectDataContext));

				func = Expression
					.Lambda<Func<DataAccessObjectDataContext, IObjectsByIdCache>>(Expression.New(constructor, Expression.Constant(dataAccessObject.GetType()), contextParam, Expression.Constant(getId, getId.GetType()), keyComparer), contextParam)
					.Compile();

				cacheConstructor = cacheConstructor.Clone(typeHandle, func);
			}

			return func(context);
		}

		public virtual DataAccessObject CacheObject(DataAccessObject value, bool forImport)
		{
			var typeHandle = Type.GetTypeHandle(value);

			if ((value.GetAdvanced().ObjectState & DataAccessObjectState.Untracked) == DataAccessObjectState.Untracked)
			{
				return value;
			}

			if (!this.cachesByType.TryGetValue(typeHandle, out var cache))
			{
				if (this.isCommiting)
				{
					Logger.Debug("Skipping caching of object {value.GetType()} because commit in process");

					return value;
				}

				cache = CreateCacheForDataAccessObject(value, this);

				this.cachesByType[typeHandle] = cache;
			}

			return cache.Cache(value, forImport);
		}

		public virtual DataAccessObject EvictObject(DataAccessObject value)
		{
			var typeHandle = Type.GetTypeHandle(value);
			
			if ((value.GetAdvanced().ObjectState & DataAccessObjectState.Untracked) == DataAccessObjectState.Untracked)
			{
				return value;
			}

			if (!this.cachesByType.TryGetValue(typeHandle, out var cache))
			{
				if (this.isCommiting)
				{
					Logger.Debug("Skipping eviction of object {value.GetType()} because commit in process");

					return value;
				}

				cache = CreateCacheForDataAccessObject(value, this);

				this.cachesByType[typeHandle] = cache;
			}

			cache.Evict(value);

			return value;
		}
		
		[RewriteAsync]
		public virtual void Commit(SqlTransactionalCommandsContext commandsContext, bool forFlush)
		{
			foreach (var cache in this.cachesByType)
			{
				cache.Value.AssertObjectsAreReadyForCommit();
			}
			
			var context = new DataAccessModelHookSubmitContext(commandsContext.TransactionContext, this, forFlush);

			try
			{
				((IDataAccessModelInternal)this.DataAccessModel).OnHookBeforeSubmit(context);

				this.isCommiting = true;

				CommitNew(commandsContext);
				CommitUpdated(commandsContext);
				CommitDeleted(commandsContext);
			}
			catch (Exception e)
			{
				context.Exception = e;

				throw;
			}
			finally
			{
				((IDataAccessModelInternal)this.DataAccessModel).OnHookAfterSubmit(context);

				this.isCommiting = false;
			}

			foreach (var cache in this.cachesByType)
			{
				cache.Value.ProcessAfterCommit();
			}
		}

		[RewriteAsync]
		private static void CommitDeleted(SqlTransactionalCommandsContext commandsContext, IObjectsByIdCache cache)
		{
			commandsContext.Delete(cache.Type, cache.GetDeletedObjects());
		}

		[RewriteAsync]
		private void CommitDeleted(SqlTransactionalCommandsContext commandsContext)
		{
			foreach (var cache in this.cachesByType)
			{
				CommitDeleted(commandsContext, cache.Value);
			}
		}

		[RewriteAsync]
		private static void CommitUpdated(SqlTransactionalCommandsContext commandsContext, IObjectsByIdCache cache)
		{
			commandsContext.Update(cache.Type, cache.GetObjectsById());
			commandsContext.Update(cache.Type, cache.GetObjectsByPredicate());
		}

		[RewriteAsync]
		private void CommitUpdated(SqlTransactionalCommandsContext commandsContext)
		{
			foreach (var cache in this.cachesByType)
			{
				CommitUpdated(commandsContext, cache.Value);
			}
		}

		[RewriteAsync]
		private static void CommitNewPhase1(SqlTransactionalCommandsContext commandsContext, IObjectsByIdCache cache, Dictionary<TypeAndTransactionalCommandsContext, InsertResults> insertResultsByType, Dictionary<TypeAndTransactionalCommandsContext, IReadOnlyList<DataAccessObject>> fixups)
		{
			var key = new TypeAndTransactionalCommandsContext(cache.Type, commandsContext);

			var currentInsertResults = commandsContext.Insert(cache.Type, cache.GetNewObjects());

			if (currentInsertResults.ToRetry.Count > 0)
			{
				insertResultsByType[key] = currentInsertResults;
			}

			if (currentInsertResults.ToFixUp.Count > 0)
			{
				fixups[key] = currentInsertResults.ToFixUp;
			}
		}

		[RewriteAsync]
		private void CommitNew(SqlTransactionalCommandsContext commandsContext)
		{
			var insertResultsByType = new Dictionary<TypeAndTransactionalCommandsContext, InsertResults>();
			var fixups = new Dictionary<TypeAndTransactionalCommandsContext, IReadOnlyList<DataAccessObject>>();
			
			foreach (var value in this.cachesByType.Values)
			{
				CommitNewPhase1(commandsContext, value, insertResultsByType, fixups);
			}

			var currentInsertResultsByType = insertResultsByType;
			var newInsertResultsByType = new Dictionary<TypeAndTransactionalCommandsContext, InsertResults>();

			while (true)
			{
				var didRetry = false;

				// Perform the retry list
				foreach (var i in currentInsertResultsByType)
				{
					var type = i.Key.Type;
					var persistenceTransactionContext = i.Key.CommandsContext;
					var retryListForType = i.Value.ToRetry;

					if (retryListForType.Count == 0)
					{
						continue;
					}

					didRetry = true;

					newInsertResultsByType[new TypeAndTransactionalCommandsContext(type, persistenceTransactionContext)] = persistenceTransactionContext.Insert(type, retryListForType);
				}

				if (!didRetry)
				{
					break;
				}

				MathUtils.Swap(ref currentInsertResultsByType, ref newInsertResultsByType);

				newInsertResultsByType.Clear();
			}

			// Perform fixups
			foreach (var i in fixups)
			{
				var type = i.Key.Type;
				var databaseTransactionContext = i.Key.CommandsContext;

				databaseTransactionContext.Update(type, i.Value);
			}
		}
	}
}
