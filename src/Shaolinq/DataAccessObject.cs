// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using Shaolinq.Persistence;

namespace Shaolinq
{
	[Serializable]
	[DataAccessObject(NotPersisted = true)]
	public abstract class DataAccessObject<T>
		: DataAccessObject
	{
		[PrimaryKey]
		[AutoIncrement]
		[PersistedMember(Name = "$(PERSISTED_TYPENAME)$(PROPERTYNAME)", SuffixName = "$(PROPERTYNAME)", PrefixName = "$(PERSISTED_TYPENAME)")]
		public abstract T Id { get; set; }
	}

	[Serializable]
	[DataAccessObject(NotPersisted = true)]
	public abstract class DataAccessObject
		: IDataAccessObjectAdvanced
	{
		// ReSharper disable once UnassignedReadonlyField
		protected internal DataAccessModel dataAccessModel;
		public DataAccessModel GetDataAccessModel() => this.dataAccessModel;

		public abstract ObjectPropertyValue[] GetAllProperties();
		public abstract bool HasPropertyChanged(string propertyName);
		public abstract List<ObjectPropertyValue> GetChangedProperties();

		public IDataAccessObjectAdvanced GetAdvanced() => this;
		public bool IsNew() => ((IDataAccessObjectAdvanced)this).IsNew;
		public bool IsDeleted() => ((IDataAccessObjectAdvanced)this).IsDeleted;
		public bool IsDeflatedReference() => ((IDataAccessObjectAdvanced)this).IsDeflatedReference;
		public SqlDatabaseContext GetDatabaseConnection() => this.GetDataAccessModel().GetCurrentSqlDatabaseContext();

		DataAccessObject IDataAccessObjectAdvanced.Inflate()
		{
			return this.Inflate();
		}

		public virtual void Delete()
		{
			this.dataAccessModel.GetCurrentDataContext(true).Deleted(this);
			this.ToObjectInternal().SetIsDeleted(true);
		}

		#region IDataAccessObjectAdvanced
		DataAccessModel IDataAccessObjectAdvanced.DataAccessModel => this.dataAccessModel;
		ObjectState IDataAccessObjectAdvanced.ObjectState { get { throw new NotImplementedException(); } }
		bool IDataAccessObjectAdvanced.DefinesAnyDirectPropertiesGeneratedOnTheServerSide => ((IDataAccessObjectAdvanced)this).NumberOfPropertiesGeneratedOnTheServerSide > 0;
		bool IDataAccessObjectAdvanced.IsNew => (((IDataAccessObjectAdvanced)this).ObjectState & ObjectState.New) != 0;
		bool IDataAccessObjectAdvanced.IsDeleted => (((IDataAccessObjectAdvanced)this).ObjectState & ObjectState.Deleted) != 0;
		bool IDataAccessObjectAdvanced.HasCompositeKey => ((IDataAccessObjectAdvanced)this).NumberOfPrimaryKeys > 1;
		bool IDataAccessObjectAdvanced.HasObjectChanged => (((IDataAccessObjectAdvanced)this).ObjectState & ObjectState.Changed) != 0;
		TypeDescriptor IDataAccessObjectAdvanced.TypeDescriptor => this.dataAccessModel.GetTypeDescriptor(this.GetType());
		Type IDataAccessObjectAdvanced.DefinitionType => this.dataAccessModel.GetDefinitionTypeFromConcreteType(this.GetType());
		#endregion

		#region Reflection emitted explicit interface implementations
		bool IDataAccessObjectAdvanced.IsMissingAnyPrimaryKeys { get { throw new NotImplementedException(); } }
		bool IDataAccessObjectAdvanced.ReferencesNewUncommitedRelatedObject { get { throw new NotImplementedException(); } }
		Type IDataAccessObjectAdvanced.KeyType { get { throw new NotImplementedException(); } }
		bool IDataAccessObjectAdvanced.IsMissingAnyDirectOrIndirectServerSideGeneratedPrimaryKeys { get { throw new NotImplementedException(); } }
		Type[] IDataAccessObjectAdvanced.CompositeKeyTypes { get { throw new NotImplementedException(); } }
		int IDataAccessObjectAdvanced.NumberOfPrimaryKeys { get { throw new NotImplementedException(); } }
		int IDataAccessObjectAdvanced.NumberOfPrimaryKeysGeneratedOnServerSide { get { throw new NotImplementedException(); } }
		bool IDataAccessObjectAdvanced.IsDeflatedReference { get { throw new NotImplementedException(); } }
		bool IDataAccessObjectAdvanced.PrimaryKeyIsCommitReady { get { throw new NotImplementedException(); } }
		int IDataAccessObjectAdvanced.NumberOfPropertiesGeneratedOnTheServerSide { get { throw new NotImplementedException(); } }
		ObjectPropertyValue[] IDataAccessObjectAdvanced.GetPrimaryKeysFlattened() { throw new NotImplementedException(); }
		ObjectPropertyValue[] IDataAccessObjectAdvanced.GetPrimaryKeysForUpdateFlattened() {  throw new NotImplementedException(); }
		ObjectPropertyValue[] IDataAccessObjectAdvanced.GetPrimaryKeys() { throw new NotImplementedException(); }
		ObjectPropertyValue[] IDataAccessObjectAdvanced.GetRelatedObjectProperties() { throw new NotImplementedException(); }
		List<ObjectPropertyValue> IDataAccessObjectAdvanced.GetChangedPropertiesFlattened() { throw new NotImplementedException(); }
		ObjectPropertyValue[] IDataAccessObjectAdvanced.GetPropertiesGeneratedOnTheServerSide() { throw new NotImplementedException(); }
		#endregion
	}
}
