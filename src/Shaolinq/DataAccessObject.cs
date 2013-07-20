using System;
using System.Collections.Generic;
using Shaolinq.Persistence;

namespace Shaolinq
{
	[Serializable]
	[DataAccessObject(Abstract = true)]
    public abstract class DataAccessObject<T>
        : IDataAccessObject
	{
		[PrimaryKey]
		[AutoIncrement]
		[PersistedMember(Name = "$(TYPENAME)$(PROPERTYNAME)", ShortName = "$(PROPERTYNAME)")]
		public abstract T Id { get; set; }

		public virtual bool IsWriteOnly
		{
			get
			{
				return ((IDataAccessObject)this).IsWriteOnly;
			}
		}

		public virtual DataAccessObject<T> UpgradeToReadWrite()
		{
			if (!((IDataAccessObject)this).IsWriteOnly)
			{
				return this;
			}

			throw new NotImplementedException();
		}

		public virtual BaseDataAccessModel DataAccessModel { get; private set; }

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

		public PersistenceContext PersistenceContext
		{
			get
			{
				return this.DataAccessModel.GetPersistenceContext(this.GetType());
			}
		}

		bool IDataAccessObject.IsNew
		{
			get
			{
				return (((IDataAccessObject)this).ObjectState & ObjectState.New) != 0;
			}
		}

		bool IDataAccessObject.IsDeleted
		{
			get
			{
				return (((IDataAccessObject)this).ObjectState & ObjectState.Deleted) != 0;
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

		#region Reflection Emitted Properties and Methods

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
		bool IDataAccessObject.DefinesAutoIncrementKey
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
		bool IDataAccessObject.HasAutoIncrementKeyValue
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

		bool IDataAccessObject.HasCompositeKey
		{
			get
			{
				return ((IDataAccessObject)this).NumberOfPrimaryKeys > 1;
			}
		}

		[ReflectionEmitted]
		public abstract bool HasPropertyChanged(string propertyName);

		[ReflectionEmitted]
		public abstract PropertyInfoAndValue[] GetPrimaryKeys();

		[ReflectionEmitted]
		public abstract List<PropertyInfoAndValue> GetChangedProperties();

		[ReflectionEmitted]
		public abstract List<PropertyInfoAndValue> GetAllProperties();

		#endregion

		#region Hidden Explicitly Implemented IDataAccessObject members

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

		void IDataAccessObject.SetDataAccessModel(BaseDataAccessModel dataAccessModel)
		{
			if (this.DataAccessModel != null)
			{
				throw new InvalidOperationException("DataAccessModel already set");
			}

			this.DataAccessModel = dataAccessModel;
		}

		[ReflectionEmitted]
		bool IDataAccessObject.IsWriteOnly
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
		void IDataAccessObject.SwapData(IDataAccessObject source)
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
		void IDataAccessObject.SetIsWriteOnly(bool value)
		{
			throw new NotImplementedException();
		}

		[ReflectionEmitted]
		void IDataAccessObject.SetIsDeleted(bool value)
		{
			throw new NotImplementedException();
		}

		[ReflectionEmitted]
		bool IDataAccessObject.ComputeIdRelatedComputedTextProperties()
		{
			throw new NotImplementedException();
		}

		#endregion
	}
}
