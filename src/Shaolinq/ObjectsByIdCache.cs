using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Platform;
using Shaolinq.TypeBuilding;

namespace Shaolinq
{
	internal interface IObjectsByIdCache
	{
		Type Type { get; }
		void ProcessAfterCommit();
		void AssertObjectsAreReadyForCommit();
		IEnumerable<DataAccessObject> GetObjectsById();
		IEnumerable<DataAccessObject> GetObjectsByPredicate();
		IEnumerable<DataAccessObject> GetNewObjects();
		IEnumerable<DataAccessObject> GetDeletedObjects();
		void Evict(DataAccessObject value);
		DataAccessObject Cache(DataAccessObject value, bool forImport);
		DataAccessObject Get(ObjectPropertyValue[] primaryKeys);
		DataAccessObject Get(LambdaExpression predicate);
		void Deleted(DataAccessObject value);
	}

	internal class ObjectsByIdCache<K>
		: IObjectsByIdCache
	{
		public Type Type { get; }
		private readonly Func<DataAccessObject, K> getIdFunc;
		private readonly DataAccessObjectDataContext dataAccessObjectDataContext;
		private HashSet<DataAccessObject> objectsNotReadyForCommit;
		private Dictionary<K, DataAccessObject> objectsDeleted;
		private readonly Dictionary<K, DataAccessObject> objectsByIdCache;
		private readonly Dictionary<DataAccessObject, DataAccessObject> newObjects;
		public IEnumerable<DataAccessObject> GetNewObjects() => this.newObjects.Values;
		public IEnumerable<DataAccessObject> GetObjectsById() => this.objectsByIdCache.Values;
		public IEnumerable<DataAccessObject> GetObjectsByPredicate() => this.objectsByPredicateCache?.Values ?? Enumerable.Empty<DataAccessObject>();
		public IEnumerable<DataAccessObject> GetDeletedObjects() => this.objectsDeleted?.Values ?? Enumerable.Empty<DataAccessObject>();
		private Dictionary<LambdaExpression, DataAccessObject> objectsByPredicateCache;

		public ObjectsByIdCache(Type type, DataAccessObjectDataContext dataAccessObjectDataContext, Func<DataAccessObject, K> getIdFunc, IEqualityComparer<K> keyComparer)
		{
			this.Type = type;
			this.getIdFunc = getIdFunc;
			this.dataAccessObjectDataContext = dataAccessObjectDataContext;
			this.objectsByIdCache = new Dictionary<K, DataAccessObject>(keyComparer ?? EqualityComparer<K>.Default);
			this.newObjects = new Dictionary<DataAccessObject, DataAccessObject>(DataAccessObjectServerSidePropertiesAccountingComparer.Default);
		}

		public void AssertObjectsAreReadyForCommit()
		{
			var x = this.objectsNotReadyForCommit?.Count;
				
			if (x == null || x ==0)
			{
				return;
			}

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
				this.objectsNotReadyForCommit?.Remove(value);
			}
			else
			{
				var id = this.getIdFunc(value);

				this.objectsByIdCache.Remove(id);
				(this.objectsDeleted = this.objectsDeleted ?? new Dictionary<K, DataAccessObject>())[id] = value;

				var internalDao = (IDataAccessObjectInternal)value;

				if (internalDao?.DeflatedPredicate != null)
				{
					this.objectsByPredicateCache?.Remove(internalDao.DeflatedPredicate);
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
			if (this.objectsByPredicateCache != null)
			{
				if (this.objectsByPredicateCache.TryGetValue(predicate, out var outValue))
				{
					return outValue;
				}
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

		public void Evict(DataAccessObject value)
		{
			if (this.dataAccessObjectDataContext.isCommiting)
			{
				return;
			}

			if (value.GetAdvanced().IsNew)
			{
				if (value.GetAdvanced().PrimaryKeyIsCommitReady)
				{
					this.newObjects.Remove(value);
				}
				else
				{
					this.objectsNotReadyForCommit?.Remove(value);
				}

				return;
			}

			var internalDao = value.ToObjectInternal();
			var predicate = internalDao?.DeflatedPredicate;

			if (predicate != null)
			{
				this.objectsByPredicateCache?.Remove(predicate);

				return;
			}

			if (value.GetAdvanced().IsMissingAnyDirectOrIndirectServerSideGeneratedPrimaryKeys)
			{
				return;
			}

			var id = this.getIdFunc(value);

			this.objectsDeleted?.Remove(id);
			this.objectsByIdCache.Remove(id);
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

					this.objectsNotReadyForCommit?.Remove(value);
						
					if (value.GetAdvanced().NumberOfPrimaryKeysGeneratedOnServerSide > 0)
					{
						return value;
					}
				}
				else
				{
					if (!(this.objectsNotReadyForCommit?.Contains(value) ?? false))
					{
						(this.objectsNotReadyForCommit = this.objectsNotReadyForCommit ?? new HashSet<DataAccessObject>(ObjectReferenceIdentityEqualityComparer<IDataAccessObjectAdvanced>.Default))
							.Add(value);
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

				if (this.objectsByPredicateCache != null)
				{
					if (this.objectsByPredicateCache.TryGetValue(predicate, out var existing))
					{
						existing.ToObjectInternal().SwapData(value, true);

						return existing;
					}
				}

				(this.objectsByPredicateCache = this.objectsByPredicateCache ?? new Dictionary<LambdaExpression, DataAccessObject>())[predicate] = value;

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
}
