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

		public virtual bool IsDeflatedReference
		{
			get
			{
				return ((IDataAccessObject)this).IsDeflatedReference;
			}
		}

		public virtual bool IsDeleted
		{
			get
			{
				return (((IDataAccessObject)this).ObjectState & ObjectState.Deleted) != 0;
			}
		}
		
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

		public virtual U As<U>()
		{
			throw new NotImplementedException();
		}

		protected void RemoveFromCache()
		{	
		}

		protected bool CanHaveNewPrimaryKey(PropertyInfoAndValue[] primaryKey)
		{
			if (this.DataAccessModel.GetCurrentDataContext(false).GetObject(this.GetType(), primaryKey) != null)
			{
				return false;
			}

			return true;
		}

		TypeDescriptor IDataAccessObject.TypeDescriptor
		{
			get
			{
				return this.DataAccessModel.GetTypeDescriptor(this.GetType());
			}
		}

		Type IDataAccessObject.DefinitionType
		{
			get
			{
				return this.DataAccessModel.GetDefinitionTypeFromConcreteType(this.GetType());
			}
		}

		public SqlDatabaseContext DatabaseConnection
		{
			get
			{
				return this.DataAccessModel.GetCurrentSqlDatabaseContext();
			}
		}

		bool IDataAccessObject.IsNew
		{
			get
			{
				return (((IDataAccessObject)this).ObjectState & ObjectState.New) != 0;
			}
		}

		public virtual U TranslateTo<U>()
		{
			return this.DataAccessModel.TranslateTo<U>(this);
		}

		public virtual void Delete()
		{
			this.DataAccessModel.GetCurrentDataContext(true).Deleted(this);
			
			((IDataAccessObject)this).SetIsDeleted(true);
		}

		bool IDataAccessObject.HasCompositeKey
		{
			get
			{
				return ((IDataAccessObject)this).NumberOfPrimaryKeys > 1;
			}
		}

		private bool isTransient;

		bool IDataAccessObject.IsTransient
		{
			get
			{
				return this.isTransient;
			}
		}

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

		[ReflectionEmitted]
		public abstract bool HasPropertyChanged(string propertyName);

		[ReflectionEmitted]
		public abstract PropertyInfoAndValue[] GetPrimaryKeys();

		[ReflectionEmitted]
		public abstract List<PropertyInfoAndValue> GetChangedProperties();

		[ReflectionEmitted]
		public abstract List<PropertyInfoAndValue> GetAllProperties();

		#region Reflection emitted explicit interface implementations

		[ReflectionEmitted]
		Type IDataAccessObject.KeyType
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		[ReflectionEmitted]
		ObjectState IDataAccessObject.ObjectState
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		[ReflectionEmitted]
		bool IDataAccessObject.DefinesAnyAutoIncrementIntegerProperties
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		[ReflectionEmitted]
		bool IDataAccessObject.HasObjectChanged
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		[ReflectionEmitted]
		bool IDataAccessObject.IsMissingAnyAutoIncrementIntegerPrimaryKeyValues 
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		[ReflectionEmitted]
		Type[] IDataAccessObject.CompositeKeyTypes
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		[ReflectionEmitted]
		int IDataAccessObject.NumberOfPrimaryKeys
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		[ReflectionEmitted]
		int IDataAccessObject.NumberOfIntegerAutoIncrementPrimaryKeys
		{
			get
			{
				throw new NotImplementedException();
			}	
		}

		[ReflectionEmitted]
		void IDataAccessObject.SetPrimaryKeys(PropertyInfoAndValue[] primaryKeys)
		{
			throw new NotImplementedException();
		}

		[ReflectionEmitted]
		object IDataAccessObject.DataObject
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		[ReflectionEmitted]
		bool IDataAccessObject.IsDeflatedReference
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		[ReflectionEmitted]
		void IDataAccessObject.ResetModified()
		{
			throw new NotImplementedException();
		}

		[ReflectionEmitted]
		void IDataAccessObject.SwapData(IDataAccessObject source, bool transferChangedProperties)
		{
			throw new NotImplementedException();
		}

		[ReflectionEmitted]
		void IDataAccessObject.SetAutoIncrementKeyValue(object value)
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
		PropertyInfo[] IDataAccessObject.GetIntegerAutoIncrementPropertyInfos()
		{
			throw new NotImplementedException();
		}

		[ReflectionEmitted]
		void IDataAccessObject.SetIntegerAutoIncrementValues(object[] values)
		{
			throw new NotImplementedException();
		}

		[ReflectionEmitted]
		bool IDataAccessObject.PrimaryKeyIsCommitReady
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		#endregion
	}
}
