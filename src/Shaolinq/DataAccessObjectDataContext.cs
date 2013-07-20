using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq.Expressions;
using Shaolinq.Persistence;
using Shaolinq.Persistence.Sql;
using Platform;
using TypeAndTcx = Platform.Pair<System.Type, Shaolinq.PersistenceTransactionContext>;

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
		#region CompositePrimaryKeyComparer

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
					if (x.keyValues[i] != y.keyValues[i])
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

		protected internal struct CompositePrimaryKey
		{
			internal readonly PropertyInfoAndValue[] keyValues;

			public CompositePrimaryKey(PropertyInfoAndValue[] keyValues)
			{
				this.keyValues = keyValues;
			}
		}

		#endregion

		#region CacheByPersistenceContext

		private class CacheByPersistenceContext<T>
			: IEnumerable<KeyValuePair<PersistenceContext, ObjectsByIdCache<T>>>
		{
			private readonly Dictionary<PersistenceContext, ObjectsByIdCache<T>> objectsByIdCacheByPersistenceContext;

			public CacheByPersistenceContext()
			{
				objectsByIdCacheByPersistenceContext = new Dictionary<PersistenceContext, ObjectsByIdCache<T>>(PrimeNumbers.Prime17);
			}

			public void UpgradeNewToUpdated()
			{
				foreach (var objectsByIdCache in objectsByIdCacheByPersistenceContext.Values)
				{
					objectsByIdCache.UpdateNewToUpdated();
				}
			}

			public void Deleted(DataAccessObject<T> value)
			{
				ObjectsByIdCache<T> objectsById;

				var persistenceContext = value.GetPersistenceContext();

				if (!this.objectsByIdCacheByPersistenceContext.TryGetValue(persistenceContext, out objectsById))
				{
					return;
				}

				objectsById.Deleted(value);
			}

			private void Foo(Expression<Func<object>> compositePrimaryKey)
			{
			}

			public DataAccessObject<T> GetById(T id)
			{
				Foo(() => new { Id = "hi"});

				return null;
			}

			public DataAccessObject<T> Get(PersistenceContext persistenceContext, Type type, PropertyInfoAndValue[] primaryKey)
			{
				ObjectsByIdCache<T> objectsById;

				if (!this.objectsByIdCacheByPersistenceContext.TryGetValue(persistenceContext, out objectsById))
				{
					objectsById = new ObjectsByIdCache<T>();

					this.objectsByIdCacheByPersistenceContext[persistenceContext] = objectsById;
				}

				return objectsById.Get(type, primaryKey);
			}

			public DataAccessObject<T> Cache(DataAccessObject<T> value, bool forImport)
			{
				ObjectsByIdCache<T> objectsById;
                
				var persistenceContext = value.GetPersistenceContext();

				if (!this.objectsByIdCacheByPersistenceContext.TryGetValue(persistenceContext, out objectsById))
				{
					objectsById = new ObjectsByIdCache<T>();

					this.objectsByIdCacheByPersistenceContext[persistenceContext] = objectsById;
				}
                
				return objectsById.Cache(value, forImport);
			}
            
			public IEnumerator<KeyValuePair<PersistenceContext, ObjectsByIdCache<T>>> GetEnumerator()
			{
				return this.objectsByIdCacheByPersistenceContext.GetEnumerator();
			}

			System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
			{
				return this.GetEnumerator();
			}
		}

		#endregion

		#region ObjectsByIdCache

		private class ObjectsByIdCache<T>
		{
			internal readonly Dictionary<Type, HashSet<IDataAccessObject>> objectsNoId;
			internal readonly Dictionary<Type, Dictionary<T, IDataAccessObject>> objectsByIdCache; 
			internal Dictionary<Type, Dictionary<T, IDataAccessObject>> objectsDeleted;
			internal Dictionary<Type, Dictionary<CompositePrimaryKey, IDataAccessObject>> objectsDeletedComposite;
			internal Dictionary<Type, Dictionary<CompositePrimaryKey, IDataAccessObject>> objectsByIdCacheComposite;
			
			public ObjectsByIdCache()
			{
				objectsNoId = new Dictionary<Type, HashSet<IDataAccessObject>>(PrimeNumbers.Prime67);
				objectsByIdCache = new Dictionary<Type, Dictionary<T, IDataAccessObject>>(PrimeNumbers.Prime67);
			}

			public void UpdateNewToUpdated()
			{
				foreach (var list in objectsNoId.Values)
				{
					foreach (DataAccessObject<T> value in list)
					{
						Debug.Assert(!((IDataAccessObject)value).IsNew);

						Cache(value, false);
					}
				}
			}

			public void Deleted(DataAccessObject<T> value)
			{
				var type = value.GetType();

				if (((IDataAccessObject)value).IsNew)
				{
					HashSet<IDataAccessObject> subcache;

					if (!objectsNoId.TryGetValue(type, out subcache))
					{
						return;
					}

					subcache.Remove(value);
				}
				else
				{
					if (((IDataAccessObject)value).NumberOfPrimaryKeys > 1)
					{
						Dictionary<CompositePrimaryKey, IDataAccessObject> subcache;
						var key = new CompositePrimaryKey(value.GetPrimaryKeys());

						if (objectsByIdCacheComposite == null)
						{
							return;
						}

						if (!objectsByIdCacheComposite.TryGetValue(type, out subcache))
						{
							return;
						}

						subcache.Remove(key);

						Dictionary<CompositePrimaryKey, IDataAccessObject> subList;

						if (objectsDeletedComposite == null)
						{
							objectsDeletedComposite = new Dictionary<Type, Dictionary<CompositePrimaryKey, IDataAccessObject>>(PrimeNumbers.Prime67);
						}
						
						if (!objectsDeletedComposite.TryGetValue(type, out subList))
						{
							subList = new Dictionary<CompositePrimaryKey, IDataAccessObject>(PrimeNumbers.Prime67, CompositePrimaryKeyComparer.Default);

							objectsDeletedComposite[type] = subList;
						}

						subList[key] = value;
					}
					else
					{
						Dictionary<T, IDataAccessObject> subcache;

						if (!objectsByIdCache.TryGetValue(type, out subcache))
						{
							return;
						}

						subcache.Remove(value.Id);

						Dictionary<T, IDataAccessObject> subList;

						if (objectsDeleted == null)
						{
							objectsDeleted = new Dictionary<Type, Dictionary<T, IDataAccessObject>>(PrimeNumbers.Prime67);
						}

						if (!objectsDeleted.TryGetValue(type, out subList))
						{
							subList = new Dictionary<T, IDataAccessObject>(PrimeNumbers.Prime127);

							objectsDeleted[type] = subList;
						}

						subList[value.Id] = value;
					}
				}
			}

			public DataAccessObject<T> Get(Type type, PropertyInfoAndValue[] primaryKeys)
			{
				IDataAccessObject outValue;

				if (primaryKeys.Length > 1)
				{
					Dictionary<CompositePrimaryKey, IDataAccessObject> subcache;

					if (this.objectsByIdCacheComposite == null)
					{
						return null;
					}

					if (!this.objectsByIdCacheComposite.TryGetValue(type, out subcache))
					{
						return null;
					}

					var key = new CompositePrimaryKey(primaryKeys);

					if (subcache.TryGetValue(key, out outValue))
					{
						return (DataAccessObject<T>)outValue;
					}

					return null;
				}
				else
				{
					Dictionary<T, IDataAccessObject> subcache;

					if (!this.objectsByIdCache.TryGetValue(type, out subcache))
					{
						return null;
					}

					if (subcache.TryGetValue((T)primaryKeys[0].value, out outValue))
					{
						return (DataAccessObject<T>)outValue;
					}

					return null;
				}
			}

			public DataAccessObject<T> Cache(DataAccessObject<T> value, bool forImport)
			{
				var type = value.GetType();

				if (((IDataAccessObject)value).IsNew)
				{
					HashSet<IDataAccessObject> subcache;

					if (!this.objectsNoId.TryGetValue(type, out subcache))
					{
						subcache = new HashSet<IDataAccessObject>(EqualityComparerUtils.ToEqualityComparer<IDataAccessObject>(Object.ReferenceEquals));

						this.objectsNoId[type] = subcache;
					}
                    
					if (!subcache.Contains(value))
					{
						subcache.Add(value);
					}

					return value;
				}
				else
				{
					if (((IDataAccessObject)value).NumberOfPrimaryKeys > 1)
					{
						Dictionary<CompositePrimaryKey, IDataAccessObject> subcache;

						var key = new CompositePrimaryKey(value.GetPrimaryKeys());

						if (this.objectsByIdCacheComposite == null)
						{
							this.objectsByIdCacheComposite = new Dictionary<Type, Dictionary<CompositePrimaryKey, IDataAccessObject>>(PrimeNumbers.Prime127);
						}

						if (!this.objectsByIdCacheComposite.TryGetValue(type, out subcache))
						{
							subcache = new Dictionary<CompositePrimaryKey, IDataAccessObject>(PrimeNumbers.Prime127, CompositePrimaryKeyComparer.Default);

							this.objectsByIdCacheComposite[type] = subcache;
						}

						if (!forImport)
						{
							IDataAccessObject outValue;

							if (subcache.TryGetValue(key, out outValue))
							{
								if (!outValue.IsWriteOnly)
								{
									outValue.SwapData(value);

									return (DataAccessObject<T>)outValue;
								}
							}
						}

						if (this.objectsDeletedComposite != null)
						{
							Dictionary<CompositePrimaryKey, IDataAccessObject> subList;

							if (this.objectsDeletedComposite.TryGetValue(type, out subList))
							{
								IDataAccessObject resurrected;

								if (subList.TryGetValue(key, out resurrected))
								{
									resurrected.SwapData(value);

									subList.Remove(key);
									subcache[key] = value;

									return (DataAccessObject<T>)resurrected;
								}
							}
						}

						subcache[key] = value;
                        
						return value;
					}
					else
					{
						var id = value.Id;
						Dictionary<T, IDataAccessObject> subcache;

						if (!this.objectsByIdCache.TryGetValue(type, out subcache))
						{
							subcache = new Dictionary<T, IDataAccessObject>(PrimeNumbers.Prime127);

							this.objectsByIdCache[type] = subcache;
						}

						if (!forImport)
						{
							IDataAccessObject outValue;

							if (subcache.TryGetValue(id, out outValue))
							{
								outValue.SwapData(value);

								return (DataAccessObject<T>)outValue;
							}
						}

						if (this.objectsDeleted != null)
						{
							Dictionary<T, IDataAccessObject> subList;

							if (this.objectsDeleted.TryGetValue(type, out subList))
							{
								IDataAccessObject resurrected;

								if (subList.TryGetValue(id, out resurrected))
								{
									if (!forImport)
									{
										resurrected.SwapData(value);

										subList.Remove(id);
										subcache[id] = value;

										return (DataAccessObject<T>)resurrected;
									}
									else
									{
										subList.Remove(id);
										subcache[id] = value;

										return value;
									}
								}
							}
						}

						subcache[value.Id] = value;

						return value;
					}
				}
			}
		}

		#endregion

		private CacheByPersistenceContext<int> cacheByInt;
		private CacheByPersistenceContext<long> cacheByLong;
		private CacheByPersistenceContext<Guid> cacheByGuid;
		private CacheByPersistenceContext<string> cacheByString;

		protected bool DisableCache { get; private set; }
		public BaseDataAccessModel DataAccessModel { get; private set; }

		public DataAccessObjectDataContext(BaseDataAccessModel dataAccessModel, bool disableCache)
		{
			this.DisableCache = disableCache;
			this.DataAccessModel = dataAccessModel;
		}

		public virtual void Deleted(IDataAccessObject value)
		{
			var keyType = value.KeyType;

			if (value.IsDeleted)
			{
				return;
			}

			if (keyType == null && value.NumberOfPrimaryKeys > 1)
			{
				keyType = value.CompositeKeyTypes[0];
			}

			switch (Type.GetTypeCode(keyType))
			{
				case TypeCode.Int32:
					if (cacheByInt == null)
					{
						cacheByInt = new CacheByPersistenceContext<int>();
					}
					cacheByInt.Deleted((DataAccessObject<int>)value);
					break;
				case TypeCode.Int64:
					if (cacheByLong == null)
					{
						cacheByLong = new CacheByPersistenceContext<long>();
					}
					cacheByLong.Deleted((DataAccessObject<long>)value);
					break;
				default:
					if (keyType == typeof(Guid))
					{
						if (cacheByGuid == null)
						{
							cacheByGuid = new CacheByPersistenceContext<Guid>();
						}
						cacheByGuid.Deleted((DataAccessObject<Guid>)value);
					}
					else if (keyType == typeof(string))
					{
						if (cacheByString == null)
						{
							cacheByString = new CacheByPersistenceContext<string>();
						}
						cacheByString.Deleted((DataAccessObject<string>)value);
					}
					break;
			}
		}

		public virtual void ImportObject(IDataAccessObject value)
		{
			if (this.DisableCache)
			{
				return;
			}

			if (value == null)
			{
				return;
			}

			value.SetTransient(false);
			ImportObject(new HashSet<IDataAccessObject>(), value);
		}

		protected void ImportObject(HashSet<IDataAccessObject> alreadyVisited, IDataAccessObject value)
		{
			if (this.DisableCache)
			{
				return;
			}

			CacheObject(value, true);

			alreadyVisited.Add(value);

			foreach (var propertyInfoAndValue in value.GetAllProperties())
			{
				var propertyValue = propertyInfoAndValue.value as IDataAccessObject;

				if (propertyValue != null && !alreadyVisited.Contains(propertyValue))
				{
					alreadyVisited.Add(propertyValue);

					ImportObject(alreadyVisited, propertyValue);
				}
			}
		}

		public virtual IDataAccessObject GetObject(PersistenceContext persistenceContext, Type type, PropertyInfoAndValue[] primaryKeys)
		{
			if (this.DisableCache)
			{
				return null;
			}

			var keyType = primaryKeys[0].propertyInfo.PropertyType;

			switch (Type.GetTypeCode(keyType))
			{
				case TypeCode.Int32:
					if (cacheByInt == null)
					{
						return null;
					}

					return cacheByInt.Get(persistenceContext, type, primaryKeys);
				case TypeCode.Int64:
					if (cacheByLong == null)
					{
						return null;
					}

					return cacheByLong.Get(persistenceContext, type, primaryKeys);
				default:
					if (keyType == typeof(Guid))
					{
						if (cacheByGuid == null)
						{
							return null;
						}

						return cacheByGuid.Get(persistenceContext, type, primaryKeys);
					}
					else if (keyType == typeof(string))
					{
						if (cacheByString == null)
						{
							return null;
						}

						return cacheByString.Get(persistenceContext, type, primaryKeys);
					}
					break;
			}

			return null;
		}

		public virtual IDataAccessObject CacheObject(IDataAccessObject value, bool forImport)
		{
			if (this.DisableCache)
			{
				return value;
			}

			var keyType = value.KeyType;

			if (keyType == null && value.NumberOfPrimaryKeys > 1)
			{
				keyType = value.CompositeKeyTypes[0];
			}

			switch (Type.GetTypeCode(keyType))
			{
				case TypeCode.Int32:
					if (cacheByInt == null)
					{
						cacheByInt = new CacheByPersistenceContext<int>();
					}
					return cacheByInt.Cache((DataAccessObject<int>)value, forImport);
				case TypeCode.Int64:
					if (cacheByLong == null)
					{
						cacheByLong = new CacheByPersistenceContext<long>();
					}
					return cacheByLong.Cache((DataAccessObject<long>)value, forImport);
				default:
					if (keyType == typeof(Guid))
					{
						if (cacheByGuid == null)
						{
							cacheByGuid = new CacheByPersistenceContext<Guid>();
						}
						return cacheByGuid.Cache((DataAccessObject<Guid>)value, forImport);
					}
					else if (keyType == typeof(string))
					{
						if (cacheByString == null)
						{
							cacheByString = new CacheByPersistenceContext<string>();
						}
						return cacheByString.Cache((DataAccessObject<string>)value, forImport);
					}
					break;
			}

			return value;
		}
		
		public virtual void Commit(TransactionContext transactionContext, bool forFlush)
		{
			var acquisitions = new HashSet<PersistenceTransactionContextAcquisition>();
			
			try
			{
				CommitNew(acquisitions, transactionContext);
				CommitUpdated(acquisitions, transactionContext);
				CommitDeleted(acquisitions, transactionContext);
				
				if (forFlush)
				{
					if (this.cacheByInt != null)
					{
						this.cacheByInt.UpgradeNewToUpdated();
					}
					
					if (this.cacheByLong != null)
					{
						this.cacheByLong.UpgradeNewToUpdated();
					}

					if (this.cacheByGuid != null)
					{
						this.cacheByGuid.UpgradeNewToUpdated();
					}

					if (this.cacheByString != null)
					{
						this.cacheByString.UpgradeNewToUpdated();
					}
				}
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

		private static void CommitDeleted<T>(CacheByPersistenceContext<T> cache, HashSet<PersistenceTransactionContextAcquisition> acquisitions, TransactionContext transactionContext)
		{
			// Insert new objects from cache
			foreach (var i in cache)
			{
				var acquisition = transactionContext.AcquirePersistenceTransactionContext(i.Key);

				acquisitions.Add(acquisition);

				if (i.Value.objectsDeleted != null)
				{
					foreach (var j in i.Value.objectsDeleted)
					{
						acquisition.PersistenceTransactionContext.Delete(j.Key, j.Value.Values);
					}
				}

				if (i.Value.objectsDeletedComposite != null)
				{
					foreach (var j in i.Value.objectsDeletedComposite)
					{
						acquisition.PersistenceTransactionContext.Delete(j.Key, j.Value.Values);
					}
				}
			}
		}

		private void CommitDeleted(HashSet<PersistenceTransactionContextAcquisition> acquisitions, TransactionContext transactionContext)
		{
			if (this.cacheByInt != null)
			{
				CommitDeleted(this.cacheByInt, acquisitions, transactionContext);
			}

			if (this.cacheByLong != null)
			{
				CommitDeleted(this.cacheByLong, acquisitions, transactionContext);
			}

			if (this.cacheByGuid != null)
			{
				CommitDeleted(this.cacheByGuid, acquisitions, transactionContext);
			}

			if (this.cacheByString != null)
			{
				CommitDeleted(this.cacheByString, acquisitions, transactionContext);
			}
		}

		private static void CommitUpdated<T>(CacheByPersistenceContext<T> cache, HashSet<PersistenceTransactionContextAcquisition> acquisitions, TransactionContext transactionContext)
	{
			// Insert new objects from cache
			foreach (var i in cache)
			{
				var acquisition = transactionContext.AcquirePersistenceTransactionContext(i.Key);

				acquisitions.Add(acquisition);

				foreach (var j in i.Value.objectsByIdCache)
				{
					acquisition.PersistenceTransactionContext.Update(j.Key, j.Value.Values);
				}

				if (i.Value.objectsByIdCacheComposite != null)
				{
					foreach (var j in i.Value.objectsByIdCacheComposite)
					{
						acquisition.PersistenceTransactionContext.Update(j.Key, j.Value.Values);
					}
				}
			}
		}

		private void CommitUpdated(HashSet<PersistenceTransactionContextAcquisition> acquisitions, TransactionContext transactionContext)
		{
			if (this.cacheByInt != null)
			{
				CommitUpdated(this.cacheByInt, acquisitions, transactionContext);
			}

			if (this.cacheByLong != null)
			{
				CommitUpdated(this.cacheByLong, acquisitions, transactionContext);
			}

			if (this.cacheByGuid != null)
			{
				CommitUpdated(this.cacheByGuid, acquisitions, transactionContext);
			}

			if (this.cacheByString != null)
			{
				CommitUpdated(this.cacheByString, acquisitions, transactionContext);
			}
		}

		private static void CommitNewPhase1<T>(HashSet<PersistenceTransactionContextAcquisition> acquisitions, CacheByPersistenceContext<T> cacheByPersistenceContext, TransactionContext transactionContext, Dictionary<TypeAndTcx, InsertResults> insertResultsByType, Dictionary<TypeAndTcx, IList<IDataAccessObject>> fixups)
		{
			// Insert new objects from cache
			foreach (var i in cacheByPersistenceContext)
			{
				var acquisition = transactionContext.AcquirePersistenceTransactionContext(i.Key);

				acquisitions.Add(acquisition);

				var persistenceTransactionContext = acquisition.PersistenceTransactionContext;

				foreach (var j in i.Value.objectsNoId)
				{
					var key = new TypeAndTcx(j.Key, persistenceTransactionContext);

					var currentInsertResults = persistenceTransactionContext.Insert(j.Key, j.Value);

					if (currentInsertResults.ToRetry.Count > 0)
					{
						insertResultsByType[key] = currentInsertResults;
					}

					if (currentInsertResults.ToFixUp.Count > 0)
					{
						fixups[key] = currentInsertResults.ToFixUp;
					}
				}
			}
		}

		private void CommitNew(HashSet<PersistenceTransactionContextAcquisition> acquisitions, TransactionContext transactionContext)
		{
			var fixups = new Dictionary<TypeAndTcx, IList<IDataAccessObject>>();
			var insertResultsByType = new Dictionary<TypeAndTcx, InsertResults>();

			if (this.cacheByInt != null)
			{
				CommitNewPhase1(acquisitions, this.cacheByInt, transactionContext, insertResultsByType, fixups);
			}

			if (this.cacheByLong != null)
			{
				CommitNewPhase1(acquisitions, this.cacheByLong, transactionContext, insertResultsByType, fixups);
			}

			if (this.cacheByGuid != null)
			{
				CommitNewPhase1(acquisitions, this.cacheByGuid, transactionContext, insertResultsByType, fixups);
			}

			if (this.cacheByString != null)
			{
				CommitNewPhase1(acquisitions, this.cacheByString, transactionContext, insertResultsByType, fixups);
			}

			var currentInsertResultsByType = insertResultsByType;
			var newInsertResultsByType = new Dictionary<TypeAndTcx, InsertResults>();

			while (true)
			{
				var didRetry = false;

                // Perform the retry list
				foreach (var i in currentInsertResultsByType)
				{
					var type = i.Key.Left;
					var persistenceTransactionContext = i.Key.Right;
					var retryListForType = i.Value.ToRetry;

					if (retryListForType.Count == 0)
					{
						continue;
					}

					didRetry = true;

					newInsertResultsByType[new TypeAndTcx(type, persistenceTransactionContext)] = persistenceTransactionContext.Insert(type, retryListForType);
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
				var type = i.Key.Left;
				var transactionPersistenceContext = i.Key.Right;

				transactionPersistenceContext.Update(type, i.Value);
			}
		}
	}
}
