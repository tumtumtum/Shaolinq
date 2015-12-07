// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Platform;
using Shaolinq.Persistence;
using Shaolinq.Persistence.Linq.Expressions;

namespace Shaolinq
{
	/// <summary>
	/// Stores a cache of all objects that have been loaded or created within a context
	/// of a transaction.
	/// Code repetition and/or ugliness in this class is due to the need for this
	/// code to run FAST.
	/// </summary>
	public class DataAccessObjectDataContext
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

		#region ObjectsByCondition

		private class ObjectsByCondition
		{
			private readonly DataAccessObjectDataContext dataAccessObjectDataContext;
			private Dictionary<ConditionalKey, DataAccessObject> objectsDeletedByCondition;
			private readonly Dictionary<ConditionalKey, DataAccessObject> objectsForUpdateByCondition;
			
			public ObjectsByCondition(DataAccessObjectDataContext dataAccessObjectDataContext)
			{
				this.dataAccessObjectDataContext = dataAccessObjectDataContext;

				this.objectsDeletedByCondition = new Dictionary<ConditionalKey, DataAccessObject>(ConditionalKeyComparer.Default);
				this.objectsForUpdateByCondition = new Dictionary<ConditionalKey, DataAccessObject>(ConditionalKeyComparer.Default);
			}

			public DataAccessObject Get(ConditionalKey key)
			{
				DataAccessObject retval;

				return this.objectsForUpdateByCondition.TryGetValue(key, out retval) ? retval : null;
			}
		}

		#endregion

		#region ObjectsByIdCache

		private interface IObjectsByIdCache
		{
			Type Type { get; }
			void ProcessAfterCommit();
			void AssertObjectsAreReadyForCommit();
			ICollection<DataAccessObject> GetObjectsById();
			ICollection<DataAccessObject> GetNewObjects();
			ICollection<DataAccessObject> GetDeletedObjects();
			DataAccessObject Cache(DataAccessObject value, bool forImport);
			DataAccessObject Get(ObjectPropertyValue[] primaryKeys);
			void Deleted(DataAccessObject value);
		}
	
		private static T GetDataAccessObjectId<T>(DataAccessObject dataAccessObject)
		{
			return ((DataAccessObject<T>)dataAccessObject).Id;
		}

		private static CompositePrimaryKey GetDataAccessObjectCompositeId(DataAccessObject dataAccessObject)
		{
			return new CompositePrimaryKey(dataAccessObject.GetAdvanced().GetPrimaryKeys());
		}

		private class ObjectsByIdCache<K>
			: IObjectsByIdCache
		{
			private readonly Func<DataAccessObject, K> getIdFunc;
			private readonly DataAccessObjectDataContext dataAccessObjectDataContext;
			private readonly HashSet<DataAccessObject> objectsNotReadyForCommit;

			private readonly Dictionary<K, DataAccessObject> objectsDeleted;
			private readonly Dictionary<K, DataAccessObject> objectsByIdCache;
			private readonly Dictionary<DataAccessObject, DataAccessObject> newObjects;

			public Type Type { get; }

			public ObjectsByIdCache(Type type, DataAccessObjectDataContext dataAccessObjectDataContext, Func<DataAccessObject, K> getIdFunc, IEqualityComparer<K> keyComparer)
			{
				this.Type = type;
				this.getIdFunc = getIdFunc;
				this.dataAccessObjectDataContext = dataAccessObjectDataContext;
				this.objectsByIdCache = new Dictionary<K, DataAccessObject>(keyComparer ?? EqualityComparer<K>.Default);
				this.objectsDeleted = new Dictionary<K, DataAccessObject>();
				this.objectsNotReadyForCommit = new HashSet<DataAccessObject>(ObjectReferenceIdentityEqualityComparer<IDataAccessObjectAdvanced>.Default);
				this.newObjects = new Dictionary<DataAccessObject, DataAccessObject>(DataAccessObjectServerSidePropertiesAccountingComparer.Default);
			}

			public ICollection<DataAccessObject> GetNewObjects() => this.newObjects.Values;
			public ICollection<DataAccessObject> GetObjectsById() => this.objectsByIdCache.Values;
			public ICollection<DataAccessObject> GetDeletedObjects() => this.objectsDeleted.Values;

			public void AssertObjectsAreReadyForCommit()
			{
				if (this.objectsNotReadyForCommit.Count == 0)
				{
					return;
				}

				if (objectsNotReadyForCommit.Count > 0)
				{
					var x = objectsNotReadyForCommit.Count;

					foreach (var value in (objectsNotReadyForCommit.Where(c => c.GetAdvanced().PrimaryKeyIsCommitReady)).ToList())
					{
						this.dataAccessObjectDataContext.CacheObject(value, false);

						x--;
					}

					if (x > 0)
					{
						var obj = objectsNotReadyForCommit.First(c => !c.GetAdvanced().PrimaryKeyIsCommitReady);

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
				}
			}

            public DataAccessObject Get(ObjectPropertyValue[] primaryKeys)
			{
				K key;
				DataAccessObject outValue;

	            if (typeof(K) == typeof(CompositePrimaryKey))
	            {
		            key = (K)(object)(new CompositePrimaryKey(primaryKeys));
	            }
	            else
	            {
		            key = (K)(object)primaryKeys[0].Value;
	            }

				if (this.objectsByIdCache.TryGetValue(key, out outValue))
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

				var dataAccessObject = (DataAccessObject)value;

				if (dataAccessObject.GetAdvanced().IsNew)
				{
					if (dataAccessObject.GetAdvanced().PrimaryKeyIsCommitReady)
					{
						DataAccessObject result;

						if (this.newObjects.TryGetValue(value, out result))
						{
							if (result != value)
							{
								throw new ObjectAlreadyExistsException(value, null, null);
							}
						}

						this.newObjects[value] = value;

						this.objectsNotReadyForCommit.Remove(value);
						
						if (dataAccessObject.GetAdvanced().NumberOfPrimaryKeysGeneratedOnServerSide > 0)
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

				if (dataAccessObject.GetAdvanced().IsMissingAnyDirectOrIndirectServerSideGeneratedPrimaryKeys)
				{
					return value;
				}
				
				var id = this.getIdFunc(value);
					
				if (!forImport)
				{
					DataAccessObject outValue;

					if (this.objectsByIdCache.TryGetValue(id, out outValue))
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
					DataAccessObject existingDeleted;

					if (this.objectsDeleted.TryGetValue(id, out existingDeleted))
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
		
		private readonly Dictionary<Type, IObjectsByIdCache> cachesByType = new Dictionary<Type, IObjectsByIdCache>();

		protected bool DisableCache { get; }
		public DataAccessModel DataAccessModel { get; }
		public SqlDatabaseContext SqlDatabaseContext { get; }

		public DataAccessObjectDataContext(DataAccessModel dataAccessModel, SqlDatabaseContext sqlDatabaseContext, bool disableCache)
		{
			this.DisableCache = disableCache;
			this.DataAccessModel = dataAccessModel;
			this.SqlDatabaseContext = sqlDatabaseContext;
		}

		public virtual void Deleted(IDataAccessObjectAdvanced value)
		{
			if (value.IsDeleted)
			{
				return;
			}

			var type = value.GetType();

			var keyType = value.KeyType;

			if (keyType == null && value.NumberOfPrimaryKeys > 1)
			{
				keyType = value.CompositeKeyTypes[0];
			}

			IObjectsByIdCache cache;

			if (!this.cachesByType.TryGetValue(type, out cache))
			{
				cache = CreateCacheForDao(value, this);

				this.cachesByType[type] = cache;
			}

			cache.Deleted((DataAccessObject)value);
		}

		public virtual void ImportObject(DataAccessObject value)
		{
			if (this.DisableCache)
			{
				return;
			}

			if (value == null)
			{
				return;
			}

			this.ImportObject(new HashSet<DataAccessObject>(), value);
		}

		protected void ImportObject(HashSet<DataAccessObject> alreadyVisited, DataAccessObject value)
		{
			if (this.DisableCache)
			{
				return;
			}

			this.CacheObject(value, true);

			alreadyVisited.Add(value);

			foreach (var propertyInfoAndValue in value.GetAdvanced().GetAllProperties())
			{
				var propertyValue = propertyInfoAndValue.Value as DataAccessObject;

				if (propertyValue != null && !alreadyVisited.Contains(propertyValue))
				{
					alreadyVisited.Add(propertyValue);

					this.ImportObject(alreadyVisited, propertyValue);
				}
			}
		}

		public virtual DataAccessObject GetObject(Type type, ObjectPropertyValue[] primaryKeys)
		{
			if (this.DisableCache)
			{
				return null;
			}

			IObjectsByIdCache cache;

			return this.cachesByType.TryGetValue(type, out cache) ? cache.Get(primaryKeys) : null;
		}

		private static Dictionary<Type, Func<DataAccessObjectDataContext, IObjectsByIdCache>> cacheConstructor = new Dictionary<Type, Func<DataAccessObjectDataContext, IObjectsByIdCache>>();

		private static IObjectsByIdCache CreateCacheForDao(IDataAccessObjectAdvanced dao, DataAccessObjectDataContext context)
		{
			var type = dao.GetType();
			Func<DataAccessObjectDataContext, IObjectsByIdCache> func;

			if (!cacheConstructor.TryGetValue(type, out func))
			{
				var keyType = dao.NumberOfPrimaryKeys > 1 ? typeof(CompositePrimaryKey) : dao.KeyType;
				var cacheType = typeof(ObjectsByIdCache<>).MakeGenericType(keyType);
				var constructor = cacheType.GetConstructors().Single();

				Delegate getIdFunc;
				Expression keyComparer;
				var getIdFuncType = typeof(Func<,>).MakeGenericType(typeof(DataAccessObject), keyType);

				if (keyType != typeof(CompositePrimaryKey))
				{
					keyComparer = Expression.Constant(null, typeof(IEqualityComparer<>).MakeGenericType(dao.KeyType ?? dao.CompositeKeyTypes[0]));
					getIdFunc = Delegate.CreateDelegate(getIdFuncType, TypeUtils.GetMethod(() => GetDataAccessObjectId<DataAccessObject>(default(DataAccessObject))).GetGenericMethodDefinition().MakeGenericMethod(dao.KeyType ?? dao.CompositeKeyTypes[0]));
				}
				else
				{
					keyComparer = Expression.Constant(CompositePrimaryKeyComparer.Default);
					getIdFunc = Delegate.CreateDelegate(getIdFuncType, TypeUtils.GetMethod(() => GetDataAccessObjectCompositeId(default(DataAccessObject))));
				}

				var contextParam = Expression.Parameter(typeof(DataAccessObjectDataContext));

				func = Expression.Lambda<Func<DataAccessObjectDataContext, IObjectsByIdCache>>(Expression.New(constructor, Expression.Constant(dao.GetType()), contextParam, Expression.Constant(getIdFunc, getIdFuncType), keyComparer), contextParam).Compile();

				var newCacheConstructor = new Dictionary<Type, Func<DataAccessObjectDataContext, IObjectsByIdCache>>(cacheConstructor) { [type] = func };

				cacheConstructor = newCacheConstructor;
			}

			return func(context);
		}

		public virtual DataAccessObject CacheObject(DataAccessObject value, bool forImport)
		{
			if (this.DisableCache)
			{
				return value;
			}

			IObjectsByIdCache cache;
			var type = value.GetType();

			if (!this.cachesByType.TryGetValue(type, out cache))
			{
				cache = CreateCacheForDao(value, this);

				this.cachesByType[type] = cache;
			}

			return cache.Cache(value, forImport);
		}

		private bool isCommiting;

		public virtual void Commit(TransactionContext transactionContext, bool forFlush)
		{
			var acquisitions = new HashSet<DatabaseTransactionContextAcquisition>();

			this.cachesByType.ForEach(c => c.Value.AssertObjectsAreReadyForCommit());

			try
			{
				try
				{
					this.isCommiting = true;

					this.CommitNew(acquisitions, transactionContext);
					this.CommitUpdated(acquisitions, transactionContext);
					this.CommitDeleted(acquisitions, transactionContext);
				}
				finally
				{
					this.isCommiting = false;
				}
				
				this.cachesByType.ForEach(c => c.Value.ProcessAfterCommit());
			}
			catch (Exception)
			{
				foreach (var acquisition in acquisitions)
				{
					acquisition.SetWasError();
				}

				throw;
			}
			finally
			{
				Exception oneException = null;

				foreach (var acquisition in acquisitions)
				{
					try
					{
						acquisition.Dispose();
					}
					catch (Exception e)
					{
						oneException = e;
					}
				}

				if (oneException != null)
				{
					throw oneException;
				}
			}
		}

		private static void CommitDeleted(SqlDatabaseContext sqlDatabaseContext, IObjectsByIdCache cache, HashSet<DatabaseTransactionContextAcquisition> acquisitions, TransactionContext transactionContext)
		{
			var acquisition = transactionContext.AcquirePersistenceTransactionContext(sqlDatabaseContext);

			acquisitions.Add(acquisition);

			acquisition.SqlDatabaseCommandsContext.Delete(cache.Type, cache.GetDeletedObjects());
		}

		private void CommitDeleted(HashSet<DatabaseTransactionContextAcquisition> acquisitions, TransactionContext transactionContext)
		{
			this.cachesByType.ForEach(c => CommitDeleted(this.SqlDatabaseContext, c.Value, acquisitions, transactionContext));
		}

		private static void CommitUpdated(SqlDatabaseContext  sqlDatabaseContext, IObjectsByIdCache cache, HashSet<DatabaseTransactionContextAcquisition> acquisitions, TransactionContext transactionContext)
		{
			var acquisition = transactionContext.AcquirePersistenceTransactionContext(sqlDatabaseContext);

			acquisitions.Add(acquisition);
			acquisition.SqlDatabaseCommandsContext.Update(cache.Type, cache.GetObjectsById());
		}

		private void CommitUpdated(HashSet<DatabaseTransactionContextAcquisition> acquisitions, TransactionContext transactionContext)
		{
			this.cachesByType.ForEach(c => CommitUpdated(this.SqlDatabaseContext, c.Value, acquisitions, transactionContext));
		}

		private static void CommitNewPhase1(SqlDatabaseContext sqlDatabaseContext, HashSet<DatabaseTransactionContextAcquisition> acquisitions, IObjectsByIdCache cache, TransactionContext transactionContext, Dictionary<TypeAndTransactionalCommandsContext, InsertResults> insertResultsByType, Dictionary<TypeAndTransactionalCommandsContext, IReadOnlyList<DataAccessObject>> fixups)
		{
			var acquisition = transactionContext.AcquirePersistenceTransactionContext(sqlDatabaseContext);

			acquisitions.Add(acquisition);

			var persistenceTransactionContext = acquisition.SqlDatabaseCommandsContext;
			var key = new TypeAndTransactionalCommandsContext(cache.Type, persistenceTransactionContext);

			var currentInsertResults = persistenceTransactionContext.Insert(cache.Type, cache.GetNewObjects());

			if (currentInsertResults.ToRetry.Count > 0)
			{
				insertResultsByType[key] = currentInsertResults;
			}

			if (currentInsertResults.ToFixUp.Count > 0)
			{
				fixups[key] = currentInsertResults.ToFixUp;
			}
		}

		private void CommitNew(HashSet<DatabaseTransactionContextAcquisition> acquisitions, TransactionContext transactionContext)
		{
			var fixups = new Dictionary<TypeAndTransactionalCommandsContext, IReadOnlyList<DataAccessObject>>();
			var insertResultsByType = new Dictionary<TypeAndTransactionalCommandsContext, InsertResults>();

			foreach (var value in this.cachesByType.Values)
			{
				CommitNewPhase1(this.SqlDatabaseContext, acquisitions, value, transactionContext, insertResultsByType, fixups);
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
