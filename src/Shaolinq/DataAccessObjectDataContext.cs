// Copyright (c) 2007-2014 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Shaolinq.Persistence;
using Platform;
using TypeAndTcx = Platform.Pair<System.Type, Shaolinq.SqlTransactionalCommandsContext>;

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
					if (!object.Equals(x.keyValues[i], y.keyValues[i]))
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
			internal readonly ObjectPropertyValue[] keyValues;

			public CompositePrimaryKey(ObjectPropertyValue[] keyValues)
			{
				this.keyValues = keyValues;
			}
		}

		#endregion

		#region ObjectsByIdCache

		internal class ObjectsByIdCache<T>
		{
			internal readonly Dictionary<Type, HashSet<IDataAccessObject>> newObjects;
			internal readonly Dictionary<Type, Dictionary<T, IDataAccessObject>> objectsByIdCache;
			internal readonly Dictionary<Type, HashSet<IDataAccessObject>> objectsNotReadyForCommit;
			internal Dictionary<Type, Dictionary<T, IDataAccessObject>> objectsDeleted;
			internal Dictionary<Type, Dictionary<CompositePrimaryKey, IDataAccessObject>> objectsDeletedComposite;
			internal Dictionary<Type, Dictionary<CompositePrimaryKey, IDataAccessObject>> objectsByIdCacheComposite;
			
			public ObjectsByIdCache()
			{
				newObjects = new Dictionary<Type, HashSet<IDataAccessObject>>(PrimeNumbers.Prime67);
				objectsByIdCache = new Dictionary<Type, Dictionary<T, IDataAccessObject>>(PrimeNumbers.Prime67);
				objectsNotReadyForCommit = new Dictionary<Type, HashSet<IDataAccessObject>>(PrimeNumbers.Prime67);
			}

			public void AssertObjectsAreReadyForCommit()
			{
				if (objectsNotReadyForCommit.Count == 0)
				{
					return;
				}

				foreach (var kvp in objectsNotReadyForCommit)
				{
					if (kvp.Value.Count > 0)
					{
						var obj = kvp.Value.First();

						throw new MissingOrInvalidPrimaryKeyException(string.Format("The object {0} is missing a primary key", obj.ToString()));
					}
				}
			}

			public void UpdateNewToUpdated()
			{
				foreach (var list in newObjects.Values)
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

					if (newObjects.TryGetValue(type, out subcache))
					{
						subcache.Remove(value);
					}

					if (objectsNotReadyForCommit.TryGetValue(type, out subcache))
					{
						subcache.Remove(value);
					}
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

			public DataAccessObject<T> Get(Type type, ObjectPropertyValue[] primaryKeys)
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

					if (subcache.TryGetValue((T)primaryKeys[0].Value, out outValue))
					{
						return (DataAccessObject<T>)outValue;
					}

					return null;
				}
			}

			public DataAccessObject<T> Cache(DataAccessObject<T> value, bool forImport)
			{
				var dataAccessObject = (IDataAccessObject)value;

				var type = value.GetType();

				if (dataAccessObject.IsNew)
				{
					HashSet<IDataAccessObject> subcache;

					if (dataAccessObject.PrimaryKeyIsCommitReady)
					{
						if (!this.newObjects.TryGetValue(type, out subcache))
						{
							subcache = new HashSet<IDataAccessObject>(IdentityEqualityComparer<IDataAccessObject>.Default);

							this.newObjects[type] = subcache;
						}

						if (!subcache.Contains(value))
						{
							subcache.Add(value);
						}

						if (this.objectsNotReadyForCommit.TryGetValue(type, out subcache))
						{
							subcache.Remove(value);
						}
						
						if (dataAccessObject.NumberOfIntegerAutoIncrementPrimaryKeys > 0)
						{
							return value;
						}
					}
					else
					{
						if (!this.objectsNotReadyForCommit.TryGetValue(type, out subcache))
						{
							subcache = new HashSet<IDataAccessObject>(IdentityEqualityComparer<IDataAccessObject>.Default);

							this.objectsNotReadyForCommit[type] = subcache;
						}

						if (!subcache.Contains(value))
						{
							subcache.Add(value);
						}

						return value;
					}
				}
				
				if (dataAccessObject.NumberOfPrimaryKeys > 1)
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
							var deleted = outValue.IsDeleted;
							
							outValue.SwapData(value, true);
							outValue.SetIsDeflatedReference(value.IsDeflatedReference);

							if (deleted)
							{
								outValue.SetIsDeleted(true);
							}

							return (DataAccessObject<T>)outValue;
						}
					}

					if (this.objectsDeletedComposite != null)
					{
						Dictionary<CompositePrimaryKey, IDataAccessObject> subList;

						if (this.objectsDeletedComposite.TryGetValue(type, out subList))
						{
							IDataAccessObject existingDeleted;

							if (subList.TryGetValue(key, out existingDeleted))
							{
								if (!forImport)
								{
									existingDeleted.SwapData(value, true);
									existingDeleted.SetIsDeleted(true);

									return (DataAccessObject<T>)existingDeleted;
								}
								else
								{
									if (value.IsDeleted)
									{
										subList[key] = value;
									}
									else
									{
										subList.Remove(key);
										subcache[key] = value;
									}

									return value;
								}
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
							var deleted = outValue.IsDeleted;

							outValue.SwapData(value, true);
							outValue.SetIsDeflatedReference(value.IsDeflatedReference);

							if (deleted)
							{
								outValue.SetIsDeleted(true);
							}

							return (DataAccessObject<T>)outValue;
						}
					}

					if (this.objectsDeleted != null)
					{
						Dictionary<T, IDataAccessObject> subList;

						if (this.objectsDeleted.TryGetValue(type, out subList))
						{
							IDataAccessObject existingDeleted;

							if (subList.TryGetValue(id, out existingDeleted))
							{
								if (!forImport)
								{
									existingDeleted.SwapData(value, true);
									existingDeleted.SetIsDeleted(true);

									return (DataAccessObject<T>)existingDeleted;
								}
								else
								{
									if (value.IsDeleted)
									{
										subList[id] = value;
									}
									else
									{
										subList.Remove(id);
										subcache[id] = value;
									}

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

		#endregion

		private ObjectsByIdCache<int> cacheByInt;
		private ObjectsByIdCache<long> cacheByLong;
		private ObjectsByIdCache<Guid> cacheByGuid;
		private ObjectsByIdCache<string> cacheByString;

		protected bool DisableCache { get; private set; }
		public DataAccessModel DataAccessModel { get; private set; }
		public SqlDatabaseContext SqlDatabaseContext { get; private set; }

		public DataAccessObjectDataContext(DataAccessModel dataAccessModel, SqlDatabaseContext sqlDatabaseContext, bool disableCache)
		{
			this.DisableCache = disableCache;
			this.DataAccessModel = dataAccessModel;
			this.SqlDatabaseContext = sqlDatabaseContext;
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
						cacheByInt = new ObjectsByIdCache<int>();
					}
					cacheByInt.Deleted((DataAccessObject<int>)value);
					break;
				case TypeCode.Int64:
					if (cacheByLong == null)
					{
						cacheByLong = new ObjectsByIdCache<long>();
					}
					cacheByLong.Deleted((DataAccessObject<long>)value);
					break;
				default:
					if (keyType == typeof(Guid))
					{
						if (cacheByGuid == null)
						{
							cacheByGuid = new ObjectsByIdCache<Guid>();
						}
						cacheByGuid.Deleted((DataAccessObject<Guid>)value);
					}
					else if (keyType == typeof(string))
					{
						if (cacheByString == null)
						{
							cacheByString = new ObjectsByIdCache<string>();
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
				var propertyValue = propertyInfoAndValue.Value as IDataAccessObject;

				if (propertyValue != null && !alreadyVisited.Contains(propertyValue))
				{
					alreadyVisited.Add(propertyValue);

					ImportObject(alreadyVisited, propertyValue);
				}
			}
		}

		public virtual IDataAccessObject GetObject(Type type, ObjectPropertyValue[] primaryKeys)
		{
			if (this.DisableCache)
			{
				return null;
			}

			var keyType = primaryKeys[0].PropertyType;

			switch (Type.GetTypeCode(keyType))
			{
				case TypeCode.Int32:
					if (cacheByInt == null)
					{
						return null;
					}

					return cacheByInt.Get(type, primaryKeys);
				case TypeCode.Int64:
					if (cacheByLong == null)
					{
						return null;
					}

					return cacheByLong.Get(type, primaryKeys);
				default:
					if (keyType == typeof(Guid))
					{
						if (cacheByGuid == null)
						{
							return null;
						}

						return cacheByGuid.Get(type, primaryKeys);
					}
					else if (keyType == typeof(string))
					{
						if (cacheByString == null)
						{
							return null;
						}

						return cacheByString.Get(type, primaryKeys);
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
						cacheByInt = new ObjectsByIdCache<int>();
					}
					return cacheByInt.Cache((DataAccessObject<int>)value, forImport);
				case TypeCode.Int64:
					if (cacheByLong == null)
					{
						cacheByLong = new ObjectsByIdCache<long>();
					}
					return cacheByLong.Cache((DataAccessObject<long>)value, forImport);
				default:
					if (keyType == typeof(Guid))
					{
						if (cacheByGuid == null)
						{
							cacheByGuid = new ObjectsByIdCache<Guid>();
						}
						return cacheByGuid.Cache((DataAccessObject<Guid>)value, forImport);
					}
					else if (keyType == typeof(string))
					{
						if (cacheByString == null)
						{
							cacheByString = new ObjectsByIdCache<string>();
						}
						return cacheByString.Cache((DataAccessObject<string>)value, forImport);
					}
					break;
			}

			return value;
		}
		
		public virtual void Commit(TransactionContext transactionContext, bool forFlush)
		{
			var acquisitions = new HashSet<DatabaseTransactionContextAcquisition>();
			
			if (this.cacheByInt != null)
			{
				this.cacheByInt.AssertObjectsAreReadyForCommit();
			}

			if (this.cacheByLong != null)
			{
				this.cacheByLong.AssertObjectsAreReadyForCommit();
			}

			if (this.cacheByGuid != null)
			{
				this.cacheByGuid.AssertObjectsAreReadyForCommit();
			}

			if (this.cacheByString != null)
			{
				this.cacheByString.AssertObjectsAreReadyForCommit();
			}

			try
			{
				CommitNew(acquisitions, transactionContext);
				CommitUpdated(acquisitions, transactionContext);
				CommitDeleted(acquisitions, transactionContext);
				
				if (forFlush)
				{
					if (this.cacheByInt != null)
					{
						this.cacheByInt.UpdateNewToUpdated();
					}
					
					if (this.cacheByLong != null)
					{
						this.cacheByLong.UpdateNewToUpdated();
					}

					if (this.cacheByGuid != null)
					{
						this.cacheByGuid.UpdateNewToUpdated();
					}

					if (this.cacheByString != null)
					{
						this.cacheByString.UpdateNewToUpdated();
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

		private static void CommitDeleted<T>(SqlDatabaseContext sqlDatabaseContext, ObjectsByIdCache<T> cache, HashSet<DatabaseTransactionContextAcquisition> acquisitions, TransactionContext transactionContext)
		{
			var acquisition = transactionContext.AcquirePersistenceTransactionContext(sqlDatabaseContext);

			acquisitions.Add(acquisition);

			if (cache.objectsDeleted != null)
			{
				foreach (var j in cache.objectsDeleted)
				{
					acquisition.SqlDatabaseCommandsContext.Delete(j.Key, j.Value.Values);
				}
			}

			if (cache.objectsDeletedComposite != null)
			{
				foreach (var j in cache.objectsDeletedComposite)
				{
					acquisition.SqlDatabaseCommandsContext.Delete(j.Key, j.Value.Values);
				}
			}
		}

		private void CommitDeleted(HashSet<DatabaseTransactionContextAcquisition> acquisitions, TransactionContext transactionContext)
		{
			if (this.cacheByInt != null)
			{
				CommitDeleted(this.SqlDatabaseContext, this.cacheByInt, acquisitions, transactionContext);
			}

			if (this.cacheByLong != null)
			{
				CommitDeleted(this.SqlDatabaseContext, this.cacheByLong, acquisitions, transactionContext);
			}

			if (this.cacheByGuid != null)
			{
				CommitDeleted(this.SqlDatabaseContext, this.cacheByGuid, acquisitions, transactionContext);
			}

			if (this.cacheByString != null)
			{
				CommitDeleted(this.SqlDatabaseContext, this.cacheByString, acquisitions, transactionContext);
			}
		}

		private static void CommitUpdated<T>(SqlDatabaseContext  sqlDatabaseContext, ObjectsByIdCache<T> cache, HashSet<DatabaseTransactionContextAcquisition> acquisitions, TransactionContext transactionContext)
		{
			var acquisition = transactionContext.AcquirePersistenceTransactionContext(sqlDatabaseContext);

			acquisitions.Add(acquisition);

			foreach (var j in cache.objectsByIdCache)
			{
				acquisition.SqlDatabaseCommandsContext.Update(j.Key, j.Value.Values);
			}

			if (cache.objectsByIdCacheComposite != null)
			{
				foreach (var j in cache.objectsByIdCacheComposite)
				{
					acquisition.SqlDatabaseCommandsContext.Update(j.Key, j.Value.Values);
				}
			}
		}

		private void CommitUpdated(HashSet<DatabaseTransactionContextAcquisition> acquisitions, TransactionContext transactionContext)
		{
			if (this.cacheByInt != null)
			{
				CommitUpdated(this.SqlDatabaseContext, this.cacheByInt, acquisitions, transactionContext);
			}

			if (this.cacheByLong != null)
			{
				CommitUpdated(this.SqlDatabaseContext, this.cacheByLong, acquisitions, transactionContext);
			}

			if (this.cacheByGuid != null)
			{
				CommitUpdated(this.SqlDatabaseContext, this.cacheByGuid, acquisitions, transactionContext);
			}

			if (this.cacheByString != null)
			{
				CommitUpdated(this.SqlDatabaseContext, this.cacheByString, acquisitions, transactionContext);
			}
		}

		private static void CommitNewPhase1<T>(SqlDatabaseContext sqlDatabaseContext, HashSet<DatabaseTransactionContextAcquisition> acquisitions, ObjectsByIdCache<T> cache, TransactionContext transactionContext, Dictionary<TypeAndTcx, InsertResults> insertResultsByType, Dictionary<TypeAndTcx, IList<IDataAccessObject>> fixups)
		{
			var acquisition = transactionContext.AcquirePersistenceTransactionContext(sqlDatabaseContext);

			acquisitions.Add(acquisition);

			var persistenceTransactionContext = acquisition.SqlDatabaseCommandsContext;

			foreach (var j in cache.newObjects)
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

		private void CommitNew(HashSet<DatabaseTransactionContextAcquisition> acquisitions, TransactionContext transactionContext)
		{
			var fixups = new Dictionary<TypeAndTcx, IList<IDataAccessObject>>();
			var insertResultsByType = new Dictionary<TypeAndTcx, InsertResults>();

			if (this.cacheByInt != null)
			{
				CommitNewPhase1(this.SqlDatabaseContext, acquisitions, this.cacheByInt, transactionContext, insertResultsByType, fixups);
			}

			if (this.cacheByLong != null)
			{
				CommitNewPhase1(this.SqlDatabaseContext, acquisitions, this.cacheByLong, transactionContext, insertResultsByType, fixups);
			}

			if (this.cacheByGuid != null)
			{
				CommitNewPhase1(this.SqlDatabaseContext, acquisitions, this.cacheByGuid, transactionContext, insertResultsByType, fixups);
			}

			if (this.cacheByString != null)
			{
				CommitNewPhase1(this.SqlDatabaseContext, acquisitions, this.cacheByString, transactionContext, insertResultsByType, fixups);
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
				var databaseTransactionContext = i.Key.Right;

				databaseTransactionContext.Update(type, i.Value);
			}
		}
	}
}
