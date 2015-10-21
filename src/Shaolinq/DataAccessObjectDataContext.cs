// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Platform.Collections;
using Shaolinq.Persistence;
using Platform;
using Shaolinq.Persistence.Linq.Expressions;
using Shaolinq.TypeBuilding;

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
				return SqlExpressionComparer.Equals(x.condition, y.condition, false);
			}

			public int GetHashCode(ConditionalKey obj)
			{
				return SqlExpressionHasher.Hash(obj.condition);
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

		#endregion

		#region ObjectsByCondition

		private class ObjectsByCondition
		{
			private readonly DataAccessObjectDataContext dataAccessObjectDataContext;

			internal Dictionary<ConditionalKey, DataAccessObject> objectsDeletedByCondition;
			internal Dictionary<ConditionalKey, DataAccessObject> objectsForUpdateByCondition;
			
			public ObjectsByCondition(DataAccessObjectDataContext dataAccessObjectDataContext)
			{
				this.dataAccessObjectDataContext = dataAccessObjectDataContext;

				this.objectsDeletedByCondition = new Dictionary<ConditionalKey, DataAccessObject>(ConditionalKeyComparer.Default);
				this.objectsForUpdateByCondition = new Dictionary<ConditionalKey, DataAccessObject>(ConditionalKeyComparer.Default);
			}

			public DataAccessObject Get(ConditionalKey key)
			{
				DataAccessObject retval;

				if (this.objectsForUpdateByCondition.TryGetValue(key, out retval))
				{
					return retval;
				}

				return null;
			}
		}

		#endregion

		#region ObjectsByIdCache

		private class ObjectsByIdCache<T>
		{
			private readonly DataAccessObjectDataContext dataAccessObjectDataContext;
			private readonly Dictionary<Type, HashSet<DataAccessObject>> objectsNotReadyForCommit;
			
			internal Dictionary<Type, Dictionary<T, DataAccessObject>> objectsDeleted;
			internal readonly Dictionary<Type, Dictionary<T, DataAccessObject>> objectsByIdCache;
			internal readonly Dictionary<Type, Dictionary<DataAccessObject, DataAccessObject>> newObjects;
			internal Dictionary<Type, Dictionary<CompositePrimaryKey, DataAccessObject>> objectsDeletedComposite;
			internal Dictionary<Type, Dictionary<CompositePrimaryKey, DataAccessObject>> objectsByIdCacheComposite;

			public ObjectsByIdCache(DataAccessObjectDataContext dataAccessObjectDataContext)
			{
				this.dataAccessObjectDataContext = dataAccessObjectDataContext;
				this.objectsByIdCache = new Dictionary<Type, Dictionary<T, DataAccessObject>>(PrimeNumbers.Prime67);
				this.objectsNotReadyForCommit = new Dictionary<Type, HashSet<DataAccessObject>>(PrimeNumbers.Prime67);
				this.newObjects = new Dictionary<Type, Dictionary<DataAccessObject, DataAccessObject>>(PrimeNumbers.Prime67);
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
						var x = kvp.Value.Count;

						foreach (var value in (kvp.Value.Where(c => c.GetAdvanced().PrimaryKeyIsCommitReady)).ToList())
						{
							dataAccessObjectDataContext.CacheObject(value, false);

							x--;
						}

						if (x > 0)
						{
							var obj = kvp.Value.First(c => !c.GetAdvanced().PrimaryKeyIsCommitReady);

							throw new MissingOrInvalidPrimaryKeyException($"The object {obj.ToString()} is missing a primary key");
						}
					}
				}
			}

			public void ProcessAfterCommit()
			{
				foreach (var value in this.newObjects.Values.SelectMany(c => c.Values))
				{
					value.ToObjectInternal().SetIsNew(false);
					value.ToObjectInternal().ResetModified();

					this.Cache((DataAccessObject<T>)value, false);
				}

				foreach (var obj in this.objectsByIdCache.SelectMany(j => j.Value.Values))
				{
					obj.ToObjectInternal().ResetModified();
				}

				if (this.objectsByIdCacheComposite != null)
				{
					foreach (var obj in this.objectsByIdCacheComposite.SelectMany(j => j.Value.Values))
					{
						obj.ToObjectInternal().ResetModified();
					}
				}

				this.newObjects.Clear();
			}

			public void Deleted(DataAccessObject<T> value)
			{
				var type = value.GetType();

				if (((IDataAccessObjectAdvanced)value).IsNew)
				{
					HashSet<DataAccessObject> notReadyForCommitSubcache;
					Dictionary<DataAccessObject, DataAccessObject> subcache;

					if (newObjects.TryGetValue(type, out subcache))
					{
						subcache.Remove(value);
					}

					if (objectsNotReadyForCommit.TryGetValue(type, out notReadyForCommitSubcache))
					{
						notReadyForCommitSubcache.Remove(value);
					}
				}
				else
				{
					if (((IDataAccessObjectAdvanced)value).NumberOfPrimaryKeys > 1)
					{
						Dictionary<CompositePrimaryKey, DataAccessObject> subcache;
						var key = new CompositePrimaryKey(value.GetAdvanced().GetPrimaryKeys());

						if (objectsByIdCacheComposite == null)
						{
							return;
						}

						if (!objectsByIdCacheComposite.TryGetValue(type, out subcache))
						{
							return;
						}

						subcache.Remove(key);

						Dictionary<CompositePrimaryKey, DataAccessObject> subList;

						if (objectsDeletedComposite == null)
						{
							objectsDeletedComposite = new Dictionary<Type, Dictionary<CompositePrimaryKey, DataAccessObject>>(PrimeNumbers.Prime67);
						}
						
						if (!objectsDeletedComposite.TryGetValue(type, out subList))
						{
							subList = new Dictionary<CompositePrimaryKey, DataAccessObject>(PrimeNumbers.Prime67, CompositePrimaryKeyComparer.Default);

							objectsDeletedComposite[type] = subList;
						}

						subList[key] = value;
					}
					else
					{
						Dictionary<T, DataAccessObject> subcache;

						if (!objectsByIdCache.TryGetValue(type, out subcache))
						{
							return;
						}

						subcache.Remove(value.Id);

						Dictionary<T, DataAccessObject> subList;

						if (objectsDeleted == null)
						{
							objectsDeleted = new Dictionary<Type, Dictionary<T, DataAccessObject>>(PrimeNumbers.Prime67);
						}

						if (!objectsDeleted.TryGetValue(type, out subList))
						{
							subList = new Dictionary<T, DataAccessObject>(PrimeNumbers.Prime127);

							objectsDeleted[type] = subList;
						}

						subList[value.Id] = value;
					}
				}
			}

			public DataAccessObject<T> Get(Type type, ObjectPropertyValue[] primaryKeys)
			{
				DataAccessObject outValue;

				if (primaryKeys.Length > 1)
				{
					Dictionary<CompositePrimaryKey, DataAccessObject> subcache;

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
					Dictionary<T, DataAccessObject> subcache;

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

			public DataAccessObject<T> Cache(DataAccessObject<T> value, bool forImport)
			{
				if (this.dataAccessObjectDataContext.isCommiting)
				{
					return value;
				}

				var dataAccessObject = (DataAccessObject)value;

				var type = value.GetType();

				if (dataAccessObject.GetAdvanced().IsNew)
				{
					HashSet<DataAccessObject> notReadyForCommitSubcache;

					if (dataAccessObject.GetAdvanced().PrimaryKeyIsCommitReady)
					{
						Dictionary<DataAccessObject, DataAccessObject> subcache;

						if (!this.newObjects.TryGetValue(type, out subcache))
						{
							subcache = new Dictionary<DataAccessObject, DataAccessObject>(DataAccessObjectServerSidePropertiesAccountingComparer.Default);

							this.newObjects[type] = subcache;
						}

						DataAccessObject result;

						if (subcache.TryGetValue(value, out result))
						{
							if (result != value)
							{
								throw new ObjectAlreadyExistsException(value, null, null);
							}
						}

						subcache[value] = value;

						if (this.objectsNotReadyForCommit.TryGetValue(type, out notReadyForCommitSubcache))
						{
							notReadyForCommitSubcache.Remove(value);
						}

						if (dataAccessObject.GetAdvanced().NumberOfPrimaryKeysGeneratedOnServerSide > 0)
						{
							return value;
						}
					}
					else
					{
						if (!this.objectsNotReadyForCommit.TryGetValue(type, out notReadyForCommitSubcache))
						{
							notReadyForCommitSubcache = new HashSet<DataAccessObject>(ObjectReferenceIdentityEqualityComparer<IDataAccessObjectAdvanced>.Default);

							this.objectsNotReadyForCommit[type] = notReadyForCommitSubcache;
						}

						if (!notReadyForCommitSubcache.Contains(value))
						{
							notReadyForCommitSubcache.Add(value);
						}

						return value;
					}
				}

				if (dataAccessObject.GetAdvanced().IsMissingAnyDirectOrIndirectServerSideGeneratedPrimaryKeys)
				{
					return value;
				}

				if (dataAccessObject.GetAdvanced().NumberOfPrimaryKeys > 1)
				{
					Dictionary<CompositePrimaryKey, DataAccessObject> subcache;

					var key = new CompositePrimaryKey(value.GetAdvanced().GetPrimaryKeys());

					if (this.objectsByIdCacheComposite == null)
					{
						this.objectsByIdCacheComposite = new Dictionary<Type, Dictionary<CompositePrimaryKey,DataAccessObject>>(PrimeNumbers.Prime127);
					}

					if (!this.objectsByIdCacheComposite.TryGetValue(type, out subcache))
					{
						subcache = new Dictionary<CompositePrimaryKey,DataAccessObject>(PrimeNumbers.Prime127, CompositePrimaryKeyComparer.Default);

						this.objectsByIdCacheComposite[type] = subcache;
					}

					if (!forImport)
					{
						DataAccessObject outValue;

						if (subcache.TryGetValue(key, out outValue))
						{
							var deleted = outValue.IsDeleted();

							outValue.ToObjectInternal().SwapData(value, true);
							
							if (deleted)
							{
								outValue.ToObjectInternal().SetIsDeleted(true);
							}

							return (DataAccessObject<T>)outValue;
						}
					}

					if (this.objectsDeletedComposite != null)
					{
						Dictionary<CompositePrimaryKey, DataAccessObject> subList;

						if (this.objectsDeletedComposite.TryGetValue(type, out subList))
						{
							DataAccessObject existingDeleted;

							if (subList.TryGetValue(key, out existingDeleted))
							{
								if (!forImport)
								{
									existingDeleted.ToObjectInternal().SwapData(value, true);
									existingDeleted.ToObjectInternal().SetIsDeleted(true);

									return (DataAccessObject<T>)existingDeleted;
								}
								else
								{
									if (value.IsDeleted())
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
					Dictionary<T, DataAccessObject> subcache;

					if (!this.objectsByIdCache.TryGetValue(type, out subcache))
					{
						subcache = new Dictionary<T, DataAccessObject>(PrimeNumbers.Prime127);

						this.objectsByIdCache[type] = subcache;
					}

					if (!forImport)
					{
						DataAccessObject outValue;

						if (subcache.TryGetValue(id, out outValue))
						{
							var deleted = outValue.IsDeleted();

							outValue.ToObjectInternal().SwapData(value, true);
							
							if (deleted)
							{
								outValue.ToObjectInternal().SetIsDeleted(true);
							}

							return (DataAccessObject<T>)outValue;
						}
					}

					if (this.objectsDeleted != null)
					{
						Dictionary<T, DataAccessObject> subList;

						if (this.objectsDeleted.TryGetValue(type, out subList))
						{
							DataAccessObject existingDeleted;

							if (subList.TryGetValue(id, out existingDeleted))
							{
								if (!forImport)
								{
									existingDeleted.ToObjectInternal().SwapData(value, true);
									existingDeleted.ToObjectInternal().SetIsDeleted(true);

									return (DataAccessObject<T>)existingDeleted;
								}
								else
								{
									if (value.IsDeleted())
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
		private ObjectsByCondition cacheByCondition;

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
					cacheByInt = new ObjectsByIdCache<int>(this);
				}
				cacheByInt.Deleted((DataAccessObject<int>)value);
				break;
			case TypeCode.Int64:
				if (cacheByLong == null)
				{
					cacheByLong = new ObjectsByIdCache<long>(this);
				}
				cacheByLong.Deleted((DataAccessObject<long>)value);
				break;
			case TypeCode.String:
				if (keyType == typeof(string))
				{
					if (cacheByString == null)
					{
						cacheByString = new ObjectsByIdCache<string>(this);
					}
					cacheByString.Deleted((DataAccessObject<string>)value);
				}
				break;
			default:
				if (keyType == typeof(Guid))
				{
					if (cacheByGuid == null)
					{
						cacheByGuid = new ObjectsByIdCache<Guid>(this);
					}
					cacheByGuid.Deleted((DataAccessObject<Guid>)value);
				}
				else if (typeof(Expression).IsAssignableFrom(keyType))
				{
					if (cacheByCondition == null)
					{
						cacheByCondition = new ObjectsByCondition(this);
					}
				}
				break;
			}
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

			ImportObject(new HashSet<DataAccessObject>(), value);
		}

		protected void ImportObject(HashSet<DataAccessObject> alreadyVisited, DataAccessObject value)
		{
			if (this.DisableCache)
			{
				return;
			}

			CacheObject(value, true);

			alreadyVisited.Add(value);

			foreach (var propertyInfoAndValue in value.GetAdvanced().GetAllProperties())
			{
				var propertyValue = propertyInfoAndValue.Value as DataAccessObject;

				if (propertyValue != null && !alreadyVisited.Contains(propertyValue))
				{
					alreadyVisited.Add(propertyValue);

					ImportObject(alreadyVisited, propertyValue);
				}
			}
		}

		public virtual DataAccessObject GetObject(Type type, ObjectPropertyValue[] primaryKeys)
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
			case TypeCode.String:
				if (cacheByString == null)
				{
					return null;
				}

				return cacheByString.Get(type, primaryKeys);
			default:
				if (keyType == typeof(Guid))
				{
					if (cacheByGuid == null)
					{
						return null;
					}

					return cacheByGuid.Get(type, primaryKeys);
				}
				else if (typeof(Expression).IsAssignableFrom(keyType))
				{
					if (cacheByCondition == null)
					{
						return null;
					}

					return cacheByCondition.Get(new ConditionalKey((Expression)primaryKeys[0].Value));
				}
				break;
			}

			return null;
		}

		public virtual DataAccessObject CacheObject(DataAccessObject value, bool forImport)
		{
			if (this.DisableCache)
			{
				return value;
			}

			var keyType = value.GetAdvanced().KeyType;

			if (keyType == null && value.GetAdvanced().NumberOfPrimaryKeys > 1)
			{
				keyType = value.GetAdvanced().CompositeKeyTypes[0];
			}

			switch (Type.GetTypeCode(keyType))
			{
			case TypeCode.Int32:
				if (cacheByInt == null)
				{
					cacheByInt = new ObjectsByIdCache<int>(this);
				}

				return cacheByInt.Cache((DataAccessObject<int>)value, forImport);
			case TypeCode.Int64:
				if (cacheByLong == null)
				{
					cacheByLong = new ObjectsByIdCache<long>(this);
				}

				return cacheByLong.Cache((DataAccessObject<long>)value, forImport);
			case TypeCode.String:
				if (cacheByString == null)
				{
					cacheByString = new ObjectsByIdCache<string>(this);
				}

				return cacheByString.Cache((DataAccessObject<string>)value, forImport);
			default:
				if (keyType == typeof(Guid))
				{
					if (cacheByGuid == null)
					{
						cacheByGuid = new ObjectsByIdCache<Guid>(this);
					}

					return cacheByGuid.Cache((DataAccessObject<Guid>)value, forImport);
				}
				break;
			}

			return value;
		}

		private bool isCommiting;

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
				try
				{
					isCommiting = true;
				
					CommitNew(acquisitions, transactionContext);
					CommitUpdated(acquisitions, transactionContext);
					CommitDeleted(acquisitions, transactionContext);
				}
				finally
				{
					isCommiting = false;
				}
				
				if (this.cacheByInt != null)
				{
					this.cacheByInt.ProcessAfterCommit();
				}
					
				if (this.cacheByLong != null)
				{
					this.cacheByLong.ProcessAfterCommit();
				}

				if (this.cacheByGuid != null)
				{
					this.cacheByGuid.ProcessAfterCommit();
				}

				if (this.cacheByString != null)
				{
					this.cacheByString.ProcessAfterCommit();
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

		private static void CommitNewPhase1<T>(SqlDatabaseContext sqlDatabaseContext, HashSet<DatabaseTransactionContextAcquisition> acquisitions, ObjectsByIdCache<T> cache, TransactionContext transactionContext, Dictionary<TypeAndTransactionalCommandsContext, InsertResults> insertResultsByType, Dictionary<TypeAndTransactionalCommandsContext, IReadOnlyList<DataAccessObject>> fixups)
		{
			var acquisition = transactionContext.AcquirePersistenceTransactionContext(sqlDatabaseContext);

			acquisitions.Add(acquisition);

			var persistenceTransactionContext = acquisition.SqlDatabaseCommandsContext;

			foreach (var j in cache.newObjects)
			{
				var key = new TypeAndTransactionalCommandsContext(j.Key, persistenceTransactionContext);

				var currentInsertResults = persistenceTransactionContext.Insert(j.Key, j.Value.Values);

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
			var fixups = new Dictionary<TypeAndTransactionalCommandsContext, IReadOnlyList<DataAccessObject>>();
			var insertResultsByType = new Dictionary<TypeAndTransactionalCommandsContext, InsertResults>();

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
