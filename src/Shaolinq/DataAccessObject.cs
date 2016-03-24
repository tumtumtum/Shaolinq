// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Linq;
using System.Collections.Generic;
using System.Linq.Expressions;
using Shaolinq.Persistence;
using Shaolinq.TypeBuilding;

namespace Shaolinq
{
	[Serializable]
	[DataAccessObject(NotPersisted = true)]
	public class DataAccessObject<T>
		: DataAccessObject
	{
		[PrimaryKey]
		[AutoIncrement]
		[PersistedMember(Name = "$(PERSISTED_TYPENAME)$(PROPERTYNAME)", SuffixName = "$(PROPERTYNAME)", PrefixName = "$(PERSISTED_TYPENAME)")]
		public virtual T Id { get; set; }
	}

	[Serializable]
	[DataAccessObject(NotPersisted = true)]
	public class DataAccessObject
		: IDataAccessObjectAdvanced
	{
		// ReSharper disable once UnassignedReadonlyField
		protected internal DataAccessModel dataAccessModel;
		public DataAccessModel GetDataAccessModel() => this.dataAccessModel;
		private TypeDescriptor TypeDescriptor => this.dataAccessModel?.GetTypeDescriptor(this.GetType());

		public virtual ObjectPropertyValue[] GetAllProperties() => this.TypeDescriptor.PersistedProperties.Select(c => ObjectPropertyValue.Create(c, this)).ToArray();
		public virtual bool HasPropertyChanged(string propertyName) => true;
		public virtual List<ObjectPropertyValue> GetChangedProperties() => this.TypeDescriptor.PersistedProperties.Select(c => ObjectPropertyValue.Create(c, this)).ToList();

		public IDataAccessObjectAdvanced GetAdvanced() => this;
		public bool IsNew() => ((IDataAccessObjectAdvanced)this).IsNew;
		DataAccessObject IDataAccessObjectAdvanced.Inflate() => this.Inflate();
		public bool IsDeleted() => ((IDataAccessObjectAdvanced)this).IsDeleted;
		public bool IsDeflatedReference() => ((IDataAccessObjectAdvanced)this).IsDeflatedReference;
		public SqlDatabaseContext GetDatabaseConnection() => this.dataAccessModel?.GetCurrentSqlDatabaseContext();

		public DataAccessObject()
		{
		}

		public DataAccessObject(DataAccessModel dataAccessModel)
		{
			this.dataAccessModel = dataAccessModel;
		}

		public virtual void Delete()
		{
			this.dataAccessModel?.GetCurrentDataContext(true).Deleted(this);
			this.ToObjectInternal()?.SetIsDeleted(true);
		}

		#region These will usually be generated with faster implementations by DataAccessObjectTypeBuilder
		DataAccessModel IDataAccessObjectAdvanced.DataAccessModel => this.dataAccessModel;
		DataAccessObjectState IDataAccessObjectAdvanced.ObjectState => DataAccessObjectState.Untracked;
		bool IDataAccessObjectAdvanced.DefinesAnyDirectPropertiesGeneratedOnTheServerSide => ((IDataAccessObjectAdvanced)this).NumberOfPropertiesGeneratedOnTheServerSide > 0;
		bool IDataAccessObjectAdvanced.IsNew => (((IDataAccessObjectAdvanced)this).ObjectState & DataAccessObjectState.New) != 0;
		bool IDataAccessObjectAdvanced.IsDeleted => (((IDataAccessObjectAdvanced)this).ObjectState & DataAccessObjectState.Deleted) != 0;
		bool IDataAccessObjectAdvanced.HasCompositeKey => ((IDataAccessObjectAdvanced)this).NumberOfPrimaryKeys > 1;
		bool IDataAccessObjectAdvanced.HasObjectChanged => (((IDataAccessObjectAdvanced)this).ObjectState & DataAccessObjectState.Changed) != 0;
		TypeDescriptor IDataAccessObjectAdvanced.TypeDescriptor => this.dataAccessModel?.GetTypeDescriptor(this.GetType());
		Type IDataAccessObjectAdvanced.DefinitionType => this.dataAccessModel?.GetDefinitionTypeFromConcreteType(this.GetType());
		bool IDataAccessObjectAdvanced.IsMissingAnyPrimaryKeys => false;
		bool IDataAccessObjectAdvanced.ReferencesNewUncommitedRelatedObject => false;
		Type IDataAccessObjectAdvanced.KeyType => ((IDataAccessObjectAdvanced)this).NumberOfPrimaryKeys != 1 ? null : this.TypeDescriptor.PrimaryKeyProperties[0].PropertyType;
		bool IDataAccessObjectAdvanced.IsMissingAnyDirectOrIndirectServerSideGeneratedPrimaryKeys => false;
		Type[] IDataAccessObjectAdvanced.CompositeKeyTypes => ((IDataAccessObjectAdvanced)this).NumberOfPrimaryKeys < 2 ? new [] { ((IDataAccessObjectAdvanced)this).KeyType } : this.TypeDescriptor.PrimaryKeyProperties.Select(c => c.PropertyType).ToArray();
		int IDataAccessObjectAdvanced.NumberOfPrimaryKeys => this.dataAccessModel?.GetTypeDescriptor(this.GetType()).PrimaryKeyCount ?? 0;
		int IDataAccessObjectAdvanced.NumberOfPrimaryKeysGeneratedOnServerSide => this.TypeDescriptor.PrimaryKeyProperties.Count(c => c.IsPropertyThatIsCreatedOnTheServerSide);
		bool IDataAccessObjectAdvanced.IsDeflatedReference => false;
		bool IDataAccessObjectAdvanced.PrimaryKeyIsCommitReady => false;
		int IDataAccessObjectAdvanced.NumberOfPropertiesGeneratedOnTheServerSide => this.TypeDescriptor.PrimaryKeyProperties.Count(c => c.IsPropertyThatIsCreatedOnTheServerSide);
		ObjectPropertyValue[] IDataAccessObjectAdvanced.GetPrimaryKeysFlattened() { throw new NotImplementedException(); }
		ObjectPropertyValue[] IDataAccessObjectAdvanced.GetPrimaryKeysForUpdateFlattened() {  throw new NotImplementedException(); }
		ObjectPropertyValue[] IDataAccessObjectAdvanced.GetPrimaryKeys() => this.TypeDescriptor.PrimaryKeyProperties.Select(c => ObjectPropertyValue.Create(c, this)).ToArray();
		ObjectPropertyValue[] IDataAccessObjectAdvanced.GetRelatedObjectProperties() => this.TypeDescriptor.PersistedProperties.Where(c => c.PropertyType.IsDataAccessObjectType()).Select(c => ObjectPropertyValue.Create(c, this)).ToArray();
		List<ObjectPropertyValue> IDataAccessObjectAdvanced.GetChangedPropertiesFlattened() { throw new NotImplementedException(); }
		ObjectPropertyValue[] IDataAccessObjectAdvanced.GetPropertiesGeneratedOnTheServerSide() => this.TypeDescriptor.PersistedProperties.Where(c => c.IsPropertyThatIsCreatedOnTheServerSide).Select(c => ObjectPropertyValue.Create(c, this)).ToArray();
		#endregion
	}
}
