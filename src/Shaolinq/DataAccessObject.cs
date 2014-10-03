// Copyright (c) 2007-2014 Thong Nguyen (tumtumtum@gmail.com)

 using System;
using System.Collections.Generic;
using System.Reflection;
using Shaolinq.Persistence;

namespace Shaolinq
{
	[Serializable]
	[DataAccessObject(NotPersisted = true)]
    public abstract class DataAccessObject<T>
        : IDataAccessObject
	{
		[PrimaryKey]
		[AutoIncrement]
		[PersistedMember(Name = "$(PERSISTEDTYPENAME)$(PROPERTYNAME)", ShortName = "$(PROPERTYNAME)")]
		public abstract T Id { get; set; }
		
		public virtual DataAccessModel DataAccessModel { get; private set; }
		public virtual bool IsDeflatedReference { get { return ((IDataAccessObject)this).IsDeflatedReference; } }
		bool IDataAccessObject.IsNew { get { return (((IDataAccessObject)this).ObjectState & ObjectState.New) != 0; } }
		public SqlDatabaseContext DatabaseConnection { get { return this.DataAccessModel.GetCurrentSqlDatabaseContext(); } }
		Type IDataAccessObject.DefinitionType { get { return this.DataAccessModel.GetDefinitionTypeFromConcreteType(this.GetType()); } }
		TypeDescriptor IDataAccessObject.TypeDescriptor { get { return this.DataAccessModel.GetTypeDescriptor(this.GetType()); } }
		public virtual bool IsDeleted { get { return (((IDataAccessObject)this).ObjectState & ObjectState.Deleted) != 0; } }
		bool IDataAccessObject.IsTransient { get { return this.isTransient; } }
		bool IDataAccessObject.HasCompositeKey { get { return ((IDataAccessObject)this).NumberOfPrimaryKeys > 1; } }
		bool IDataAccessObject.HasObjectChanged { get { return (((IDataAccessObject)this).ObjectState & ObjectState.Changed) != 0; } }

		
		public virtual void Inflate()
		{
			if (!((IDataAccessObject)this).IsDeflatedReference)
			{
				return;
			}

			var inflated = this.DataAccessModel.Inflate((IDataAccessObject)this);

			// SwapData should not be necessary inside a transaction 
			((IDataAccessObject)this).SwapData(inflated, true);

			((IDataAccessObject)this).SetIsDeflatedReference(false);
		}

		protected void RemoveFromCache()
		{	
		}

		protected bool CanHaveNewPrimaryKey(ObjectPropertyValue[] primaryKey)
		{
			if (this.DataAccessModel.GetCurrentDataContext(false).GetObject(this.GetType(), primaryKey) != null)
			{
				return false;
			}

			return true;
		}

		public virtual void Delete()
		{
			this.DataAccessModel.GetCurrentDataContext(true).Deleted(this);
			
			((IDataAccessObject)this).SetIsDeleted(true);
		}

		private bool isTransient;

		void IDataAccessObject.SetTransient(bool transient)
		{
			this.isTransient = transient;
		}

		void IDataAccessObject.SetDataAccessModel(DataAccessModel dataAccessModel)
		{
			if (this.DataAccessModel != null)
			{
				throw new InvalidOperationException("DataAccessModel already set");
			}

			this.DataAccessModel = dataAccessModel;
		}

		bool IDataAccessObject.DefinesAnyDirectPropertiesGeneratedOnTheServerSide { get { return ((IDataAccessObject)this).NumberOfDirectPropertiesGeneratedOnTheServerSide > 0; } }

		[ReflectionEmitted]
		public abstract bool HasPropertyChanged(string propertyName);

		[ReflectionEmitted]
		public abstract ObjectPropertyValue[] GetPrimaryKeys();

		[ReflectionEmitted]
		public abstract ObjectPropertyValue[] GetPrimaryKeysFlattened();

		[ReflectionEmitted]
		public abstract ObjectPropertyValue[] GetPrimaryKeysForUpdateFlattened();
		
		[ReflectionEmitted]
		public abstract ObjectPropertyValue[] GetAllProperties();
		
		[ReflectionEmitted]
		public abstract ObjectPropertyValue[] GetRelatedObjectProperties();

		[ReflectionEmitted]
		public abstract List<ObjectPropertyValue> GetChangedProperties();

		[ReflectionEmitted]
		public abstract List<ObjectPropertyValue> GetChangedPropertiesFlattened();

		#region Reflection emitted explicit interface implementations

		[ReflectionEmitted]
		Type IDataAccessObject.KeyType { get { throw new NotImplementedException(); } }

		[ReflectionEmitted]
		ObjectState IDataAccessObject.ObjectState { get { throw new NotImplementedException(); } }

		[ReflectionEmitted]
		bool IDataAccessObject.IsMissingAnyDirectOrIndirectServerSideGeneratedPrimaryKeys { get { throw new NotImplementedException(); } }

		[ReflectionEmitted]
		Type[] IDataAccessObject.CompositeKeyTypes { get { throw new NotImplementedException(); } }

		[ReflectionEmitted]
		int IDataAccessObject.NumberOfPrimaryKeys { get { throw new NotImplementedException(); } }

		[ReflectionEmitted]
		int IDataAccessObject.NumberOfPrimaryKeysGeneratedOnServerSide { get { throw new NotImplementedException(); } }

		[ReflectionEmitted]
		void IDataAccessObject.SetPrimaryKeys(ObjectPropertyValue[] primaryKeys)
		{
			throw new NotImplementedException();
		}

		[ReflectionEmitted]
		bool IDataAccessObject.IsDeflatedReference { get { throw new NotImplementedException(); } }

		[ReflectionEmitted]
		IDataAccessObject IDataAccessObject.ResetModified()
		{
			throw new NotImplementedException();
		}

		[ReflectionEmitted]
		IDataAccessObject IDataAccessObject.FinishedInitializing()
		{
			throw new NotImplementedException();
		}

		[ReflectionEmitted]
		void IDataAccessObject.SwapData(IDataAccessObject source, bool transferChangedProperties)
		{
			throw new NotImplementedException();
		}

		[ReflectionEmitted]
		void IDataAccessObject.SetIsNew(bool value)
		{
			throw new NotImplementedException();
		}

		[ReflectionEmitted]
		void IDataAccessObject.SetIsDeflatedReference(bool value)
		{
			throw new NotImplementedException();
		}

		[ReflectionEmitted]
		void IDataAccessObject.SetIsDeleted(bool value)
		{
			throw new NotImplementedException();
		}

		[ReflectionEmitted]
		bool IDataAccessObject.ComputeServerGeneratedIdDependentComputedTextProperties()
		{
			throw new NotImplementedException();
		}

		[ReflectionEmitted]
		ObjectPropertyValue[] IDataAccessObject.GetPropertiesGeneratedOnTheServerSide()
		{
			throw new NotImplementedException();
		}

		[ReflectionEmitted]
		void IDataAccessObject.SetPropertiesGeneratedOnTheServerSide(object[] values)
		{
			throw new NotImplementedException();
		}

		[ReflectionEmitted]
		bool IDataAccessObject.PrimaryKeyIsCommitReady { get { throw new NotImplementedException(); } }

		[ReflectionEmitted]
		int IDataAccessObject.NumberOfDirectPropertiesGeneratedOnTheServerSide { get { throw new NotImplementedException(); } }

		#endregion

		IDataAccessObject IDataAccessObject.SubmitToCache()
		{
			return this.DataAccessModel.GetCurrentDataContext(false).CacheObject(this, false);
		}
	}
}
