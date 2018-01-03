// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Platform;
using Shaolinq.Persistence;
using Shaolinq.Persistence.Linq.Expressions;
using Shaolinq.TypeBuilding;

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

		#region ConditionObjectIdentifier
		protected internal struct ConditionalKey
		{
			internal readonly Expression condition;

			public ConditionalKey(Expression condition)
			{
				this.condition = condition;
			}
		}

		protected internal class ConditionalKeyComparer
			: IEqualityComparer<ConditionalKey>
		{
			public static readonly ConditionalKeyComparer Default = new ConditionalKeyComparer();

			public bool Equals(ConditionalKey x, ConditionalKey y)
			{
				return SqlExpressionComparer.Equals(x.condition, y.condition, SqlExpressionComparerOptions.None);
			}

			public int GetHashCode(ConditionalKey obj)
			{
				return SqlExpressionHasher.Hash(obj.condition, SqlExpressionComparerOptions.None);
			}
		}
		#endregion

		protected internal struct PredicatePrimaryKey
		{
			internal readonly LambdaExpression predicate;

			public PredicatePrimaryKey(LambdaExpression predicate)
			{
				this.predicate = predicate;
			}
		}

		protected internal class PredicatePrimaryKeyComparer
			: IEqualityComparer<PredicatePrimaryKey>
		{
			public static readonly PredicatePrimaryKeyComparer Default = new PredicatePrimaryKeyComparer();

			public bool Equals(PredicatePrimaryKey x, PredicatePrimaryKey y)
			{
				return SqlExpressionComparer.Equals(x.predicate, y.predicate);
			}

			public int GetHashCode(PredicatePrimaryKey obj)
			{
				return SqlExpressionHasher.Hash(obj.predicate);
			}
		}

		#region CompositePrimaryKey

		protected internal struct CompositePrimaryKey
		{
			internal readonly ObjectPropertyValue[] keyValues;

			public CompositePrimaryKey(ObjectPropertyValue[] keyValues)
			{
				this.keyValues = keyValues;
			}
		}

		protected internal class CompositePrimaryKeyComparer
			: IEqualityComparer<CompositePrimaryKey>
		{
			public static readonly CompositePrimaryKeyComparer Default = new CompositePrimaryKeyComparer();
			
			public bool Equals(CompositePrimaryKey x, CompositePrimaryKey y)
			{
				if (x.keyValues.Length != y.keyValues.Length)
				{
					return false;
				}

				for (int i = 0, n = x.keyValues.Length; i < n; i++)
				{
					if (!Equals(x.keyValues[i], y.keyValues[i]))
					{
						return false;
					}
				}

				return true;
			}

			public int GetHashCode(CompositePrimaryKey obj)
			{
				var retval = obj.keyValues.Length;

				for (int i = 0, n = Math.Min(retval, 8); i < n;  i++)
				{
					retval ^= obj.keyValues[i].GetHashCode();
				}

				return retval;
			}
		}

		#endregion

		#region ObjectsByIdCache

		internal interface IObjectsByIdCache
		{
			Type Type { get; }
			void ProcessAfterCommit();
			void AssertObjectsAreReadyForCommit();
			ICollection<DataAccessObject> GetObjectsById();
			ICollection<DataAccessObject> GetObjectsByPredicate();
			ICollection<DataAccessObject> GetNewObjects();
			ICollection<DataAccessObject> GetDeletedObjects();
			DataAccessObject Cache(DataAccessObject value, bool forImport);
			DataAccessObject Get(ObjectPropertyValue[] primaryKeys);
			DataAccessObject Get(LambdaExpression predicate);
			void Deleted(DataAccessObject value);
		}

		private static CompositePrimaryKey GetDataAccessObjectCompositeId(DataAccessObject dataAccessObject)
		{
			return new CompositePrimaryKey(dataAccessObject.GetAdvanced().GetPrimaryKeys());
		}
		
		private class ObjectsByIdCache<K>
			: IObjectsByIdCache
		{
			public Type Type { get; }
			private readonly Func<DataAccessObject, K> getIdFunc;
			private readonly DataAccessObjectDataContext dataAccessObjectDataContext;
			private readonly HashSet<DataAccessObject> objectsNotReadyForCommit;
			private readonly Dictionary<K, DataAccessObject> objectsDeleted;
			private readonly Dictionary<K, DataAccessObject> objectsByIdCache;
			private readonly Dictionary<DataAccessObject, DataAccessObject> newObjects;
			public ICollection<DataAccessObject> GetNewObjects() => this.newObjects.Values;
			public ICollection<DataAccessObject> GetObjectsById() => this.objectsByIdCache.Values;
			public ICollection<DataAccessObject> GetObjectsByPredicate() => this.objectsByPredicateCache.Values;
			public ICollection<DataAccessObject> GetDeletedObjects() => this.objectsDeleted.Values;
			private readonly Dictionary<LambdaExpression, DataAccessObject> objectsByPredicateCache;

			public ObjectsByIdCache(Type type, DataAccessObjectDataContext dataAccessObjectDataContext, Func<DataAccessObject, K> getIdFunc, IEqualityComparer<K> keyComparer)
			{
				this.Type = type;
				this.getIdFunc = getIdFunc;
				this.dataAccessObjectDataContext = dataAccessObjectDataContext;
				this.objectsByIdCache = new Dictionary<K, DataAccessObject>(keyComparer ?? EqualityComparer<K>.Default);
				this.objectsDeleted = new Dictionary<K, DataAccessObject>();
				this.objectsByPredicateCache = new Dictionary<LambdaExpression, DataAccessObject>();
				this.objectsNotReadyForCommit = new HashSet<DataAccessObject>(ObjectReferenceIdentityEqualityComparer<IDataAccessObjectAdvanced>.Default);
				this.newObjects = new Dictionary<DataAccessObject, DataAccessObject>(DataAccessObjectServerSidePropertiesAccountingComparer.Default);
			}

			public void AssertObjectsAreReadyForCommit()
			{
				if (this.objectsNotReadyForCommit.Count == 0)
				{
					return;
				}

				if (this.objectsNotReadyForCommit.Count > 0)
				{
					var x = this.objectsNotReadyForCommit.Count;

					foreach (var value in (this.objectsNotReadyForCommit.Where(c => c.GetAdvanced().PrimaryKeyIsCommitReady)).ToList())
					{
						this.dataAccessObjectDataContext.CacheObject(value, false);

						x--;
					}

					if (x > 0)
					{
						var obj = this.objectsNotReadyForCommit.First(c => !c.GetAdvanced().PrimaryKeyIsCommitReady);

						throw new MissingOrInvalidPrimaryKeyException($"The object {obj} is missing a primary key");
					}
				}
			}

			public void ProcessAfterCommit()
			{
				foreach (var value in this.newObjects.Values)
				{
					value.ToObjectInternal().SetIsNew(false);
					value.ToObjectInternal().ResetModified();

					this.Cache(value, false);
				}

				foreach (var obj in this.objectsByIdCache.Values)
				{
					obj.ToObjectInternal().ResetModified();
				}

				this.newObjects.Clear();
			}
			
			public void Deleted(DataAccessObject value)
			{
				if (((IDataAccessObjectAdvanced)value).IsNew)
				{
					this.newObjects.Remove(value);
					this.objectsNotReadyForCommit.Remove(value);
				}
				else
				{
					var id = this.getIdFunc(value);

					this.objectsByIdCache.Remove(id);
					this.objectsDeleted[id] = value;

					var internalDao = (IDataAccessObjectInternal)value;

					if (internalDao?.DeflatedPredicate != null)
					{
						this.objectsByPredicateCache.Remove(internalDao.DeflatedPredicate);
					}
				}
			}

			public DataAccessObject Get(ObjectPropertyValue[] primaryKeys)
			{
				K key;

				if (typeof(K) == typeof(CompositePrimaryKey))
				{
					key = (K)(object)(new CompositePrimaryKey(primaryKeys));
				}
				else
				{
					key = (K)primaryKeys[0].Value;
				}

				if (this.objectsByIdCache.TryGetValue(key, out var outValue))
				{
					return outValue;
				}

				return null;
			}

			public DataAccessObject Get(LambdaExpression predicate)
			{
				if (this.objectsByPredicateCache.TryGetValue(predicate, out var outValue))
				{
					return outValue;
				}

				return null;
			}

			private class DataAccessObjectServerSidePropertiesAccountingComparer
				: IEqualityComparer<DataAccessObject>
			{
				internal static readonly DataAccessObjectServerSidePropertiesAccountingComparer Default = new DataAccessObjectServerSidePropertiesAccountingComparer();

				public bool Equals(DataAccessObject x, DataAccessObject y)
				{
					return x.ToObjectInternal().EqualsAccountForServerGenerated(y);
				}

				public int GetHashCode(DataAccessObject obj)
				{
					return obj.ToObjectInternal().GetHashCodeAccountForServerGenerated();
				}
			}

			public DataAccessObject Cache(DataAccessObject value, bool forImport)
			{
				if (this.dataAccessObjectDataContext.isCommiting)
				{
					return value;
				}

				if (value.GetAdvanced().IsNew)
				{
					if (value.GetAdvanced().PrimaryKeyIsCommitReady)
					{
						if (this.newObjects.TryGetValue(value, out var result))
						{
							if (result != value)
							{
								throw new ObjectAlreadyExistsException(value, null, null);
							}
						}

						this.newObjects[value] = value;

						this.objectsNotReadyForCommit.Remove(value);
						
						if (value.GetAdvanced().NumberOfPrimaryKeysGeneratedOnServerSide > 0)
						{
							return value;
						}
					}
					else
					{
						if (!this.objectsNotReadyForCommit.Contains(value))
						{
							this.objectsNotReadyForCommit.Add(value);
						}

						return value;
					}
				}

				var internalDao = value.ToObjectInternal();
				var predicate = internalDao?.DeflatedPredicate;

				if (predicate != null)
				{
					if (forImport)
					{
						throw new InvalidOperationException("Cannot import predicated deflated object");
					}
					
					if (this.objectsByPredicateCache.TryGetValue(predicate, out var existing))
					{
						existing.ToObjectInternal().SwapData(value, true);

						return existing;
					}

					this.objectsByPredicateCache[predicate] = value;

					return value;
				}

				if (value.GetAdvanced().IsMissingAnyDirectOrIndirectServerSideGeneratedPrimaryKeys)
				{
					return value;
				}
				
				var id = this.getIdFunc(value);
					
				if (!forImport)
				{
					if (this.objectsByIdCache.TryGetValue(id, out var outValue))
					{
						var deleted = outValue.IsDeleted();

						outValue.ToObjectInternal().SwapData(value, true);

						if (deleted)
						{
							outValue.ToObjectInternal().SetIsDeleted(true);
						}

						return outValue;
					}
				}

				if (this.objectsDeleted != null)
				{
					if (this.objectsDeleted.TryGetValue(id, out var existingDeleted))
					{
						if (!forImport)
						{
							existingDeleted.ToObjectInternal().SwapData(value, true);
							existingDeleted.ToObjectInternal().SetIsDeleted(true);

							return existingDeleted;
						}
						else
						{
							if (value.IsDeleted())
							{
								this.objectsDeleted[id] = value;
							}
							else
							{
								this.objectsDeleted.Remove(id);
								this.objectsByIdCache[id] = value;
							}

							return value;
						}
					}
				}

				this.objectsByIdCache[id] = value;

				return value;
			}
		}

		#endregion
		
		internal readonly Dictionary<RuntimeTypeHandle, IObjectsByIdCache> cachesByType = new Dictionary<RuntimeTypeHandle, IObjectsByIdCache>();
		
		private bool isCommiting;
		public DataAccessModel DataAccessModel { get; }
		public SqlDatabaseContext SqlDatabaseContext { get; }

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
				cache = CreateCacheForDao(value, this);

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

			this.ImportObject(new HashSet<DataAccessObject>(), value);
		}

		protected void ImportObject(HashSet<DataAccessObject> alreadyVisited, DataAccessObject value)
		{
			this.CacheObject(value, true);

			alreadyVisited.Add(value);

			foreach (var propertyInfoAndValue in value.GetAdvanced().GetAllProperties())
			{

				if (propertyInfoAndValue.Value is DataAccessObject propertyValue && !alreadyVisited.Contains(propertyValue))
				{
					alreadyVisited.Add(propertyValue);

					this.ImportObject(alreadyVisited, propertyValue);
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

		private static IObjectsByIdCache CreateCacheForDao(IDataAccessObjectAdvanced dao, DataAccessObjectDataContext context)
		{
			var typeHandle = Type.GetTypeHandle(dao);

			if (!cacheConstructor.TryGetValue(typeHandle, out var func))
			{
				var type = dao.GetType();

				var keyType = dao.NumberOfPrimaryKeys > 1 ? typeof(CompositePrimaryKey) : dao.KeyType;
				var cacheType = typeof(ObjectsByIdCache<>).MakeGenericType(keyType);
				var constructor = cacheType.GetConstructors().Single();

				Delegate getIdFunc;
				Expression keyComparer;
				var getIdFuncType = typeof(Func<,>).MakeGenericType(typeof(DataAccessObject), keyType);

				if (keyType != typeof(CompositePrimaryKey))
				{
					keyComparer = Expression.Constant(null, typeof(IEqualityComparer<>).MakeGenericType(dao.KeyType ?? dao.CompositeKeyTypes[0]));

					var param = Expression.Parameter(typeof(DataAccessObject));
					var lambda = Expression.Lambda(dao.TypeDescriptor.GetSinglePrimaryKeyExpression(Expression.Convert(param, type)), param);

					getIdFunc = lambda.Compile();
				}
				else
				{
					keyComparer = Expression.Constant(CompositePrimaryKeyComparer.Default);
					getIdFunc = Delegate.CreateDelegate(getIdFuncType, TypeUtils.GetMethod(() => GetDataAccessObjectCompositeId(default(DataAccessObject))));
				}

				var contextParam = Expression.Parameter(typeof(DataAccessObjectDataContext));

				func = Expression.Lambda<Func<DataAccessObjectDataContext, IObjectsByIdCache>>(Expression.New(constructor, Expression.Constant(dao.GetType()), contextParam, Expression.Constant(getIdFunc, getIdFunc.GetType()), keyComparer), contextParam).Compile();

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
				cache = CreateCacheForDao(value, this);

				this.cachesByType[typeHandle] = cache;
			}

			return cache.Cache(value, forImport);
		}
		
		[RewriteAsync]
		public virtual void Commit(SqlTransactionalCommandsContext commandsContext, bool forFlush)
		{
			foreach (var cache in this.cachesByType)
			{
				cache.Value.AssertObjectsAreReadyForCommit();
			}
			
			try
			{
				var context = new DataAccessModelHookSubmitContext(this, forFlush);

				this.DataAccessModel.OnHookBeforeSubmit(context);

				this.isCommiting = true;
				
				this.CommitNew(commandsContext);
				this.CommitUpdated(commandsContext);
				this.CommitDeleted(commandsContext);

				this.DataAccessModel.OnHookAfterSubmit(context);
			}
			finally
			{
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
			var fixups = new Dictionary<TypeAndTransactionalCommandsContext, IReadOnlyList<DataAccessObject>>();
			var insertResultsByType = new Dictionary<TypeAndTransactionalCommandsContext, InsertResults>();

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
