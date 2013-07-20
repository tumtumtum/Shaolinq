using System;
using System.Collections.Generic;
using Shaolinq.Persistence;

namespace Shaolinq
{
	/// <summary>
	/// An interface implemented by all data access objects.
	/// </summary>
	public interface IDataAccessObject
	{
		/// <summary>
		/// The TypeDescriptor associated with this data access object.
		/// </summary>
		TypeDescriptor TypeDescriptor { get; }

		/// <summary>
		/// Gets the data access model associated with the current domain object.
		/// </summary>
		BaseDataAccessModel DataAccessModel { get; }

		/// <summary>
		/// Returns true if the current object has only been partially loaded and can't be read.
		/// </summary>
		bool IsWriteOnly { get; }

		/// <summary>
		/// Returns true if the current object has not yet been persisted or flushed.
		/// </summary>
		bool IsNew { get; }

		/// <summary>
		/// Returns true if the current object has been deleted
		/// </summary>
		bool IsDeleted { get; }

		/// <summary>
		/// Returns the Primary Key type for this object.  If the object defines a composite key the return value will be typeof(object).
		/// </summary>
		Type KeyType { get; }

		/// <summary>
		/// Returns an array of the types in the composite primary key
		/// </summary>
		Type[] CompositeKeyTypes { get; }

		/// <summary>
		/// Returns the number of keys that make up the primary key
		/// </summary>
		int NumberOfPrimaryKeys { get; }

		/// <summary>
		/// Returns True if the object has a composite primary key
		/// </summary>
		bool HasCompositeKey { get; }

		/// <summary>
		/// Returns true if the object has been changed from when it was first loaded or created.
		/// </summary>
		bool HasObjectChanged { get; }

		/// <summary>
		/// Gets the object state of the object (mostly used for persistence).
		/// </summary>
		ObjectState ObjectState { get; }

		/// <summary>
		/// Gets the original abstract type that defined the current object.
		/// </summary>
		Type DefinitionType { get; }

		/// <summary>
		/// Resets the modified status of all the properties that aren't unrealised
		/// foreign key references.
		/// </summary>
		void ResetModified();

		/// <summary>
		/// Returns true if the object has an auto-incrementing key
		/// </summary>
		bool HasAutoIncrementKeyValue
		{
			get;
		}

		/// <summary>
		/// Returns True if the current object defines a primary key that is an autoincrement key.
		/// </summary>
		bool DefinesAutoIncrementKey
		{
			get;
		}

		U TranslateTo<U>();

		bool IsTransient
		{
			get;
		}

		void SetTransient(bool transient);

		void Delete();

		/// <summary>
		/// Marks this object as newly created (unpersisted).
		/// </summary>
		void SetIsNew(bool value);

		/// <summary>
		/// Marks this object has been deleted.
		/// </summary>
		void SetIsDeleted(bool value);

		void SetIsWriteOnly(bool isWriteOnly);

		/// <summary>
		/// Sets the associated model of this object.
		/// </summary>
		void SetDataAccessModel(BaseDataAccessModel dataAccessModel);

		/// <summary>
		/// Sets the underlying data container of the current data access object with the one in the given domain object.
		/// </summary>
		void SwapData(IDataAccessObject source);

		/// <summary>
		/// Sets the AutoIncrement key value of this object.
		/// </summary>
		void SetAutoIncrementKeyValue(object value);

		/// <summary>
		/// Returns true if the property with the given name has been changed since the object was loaded or created.
		/// </summary>
		bool HasPropertyChanged(string propertyName);
        
		/// <summary>
		/// Gets an array of the primary keys and their values.
		/// This property is generated using Reflection.Emit.  Strings inside the returned <see cref="PropertyInfoAndValue"/>
		/// are guaranteed to be interned.
		/// </summary>
		PropertyInfoAndValue[] GetPrimaryKeys();

		/// <summary>
		/// Gets a list of all the properties on this object that have changed since the object was loaded or created.
		/// This property is generated using Reflection.Emit.  Strings inside the returned <see cref="PropertyInfoAndValue"/>
		/// are guaranteed to be interned.
		/// </summary>
		List<PropertyInfoAndValue> GetChangedProperties();

		/// <summary>
		/// Gets a list of all the properties on this object
		/// </summary>
		List<PropertyInfoAndValue> GetAllProperties();

		/// <summary>
		/// Gets a list of all the properties on this object
		/// </summary>
		bool ComputeIdRelatedComputedTextProperties();
	}
}
