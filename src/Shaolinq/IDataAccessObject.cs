using System;
using System.Collections.Generic;
using System.Reflection;
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
		bool IsDeflatedReference { get; }

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

		int NumberOfIntegerAutoIncrementPrimaryKeys { get; }

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
		/// </summary>S
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
		//bool HasAutoIncrementKeyValue { get; }

		/// <summary>
		/// Returns True if the current object defines a primary key that is an autoincrement key.
		/// </summary>
		//bool DefinesAutoIncrementKey { get; }

		bool DefinesAnyAutoIncrementIntegerProperties { get; }

		/// <summary>
		/// Returns True if the object is missing any auto increment integer primary keys
		/// </summary>
		bool IsMissingAnyAutoIncrementIntegerPrimaryKeyValues { get; }

		/// <summary>
		/// Converts this object to another type using name/convention mapping
		/// </summary>
		U TranslateTo<U>();

		/// <summary>
		/// Returns true if the current object doesn't belong to any transaction (is freeform)
		/// </summary>
		bool IsTransient { get; }

		/// <summary>
		/// Makes this object as not belonging to any transaction (it will not be commited)
		/// </summary>
		void SetTransient(bool transient);

		/// <summary>
		/// Deletes the object.
		/// </summary>
		void Delete();

		/// <summary>
		/// Marks this object as newly created (unpersisted).
		/// </summary>
		void SetIsNew(bool value);

		/// <summary>
		/// Marks this object has been deleted.
		/// </summary>
		void SetIsDeleted(bool value);

		/// <summary>
		/// Makes the object as write-only (reads can only be made to primary key properties and properties that have been
		/// changed within the context of the current transaction)
		/// </summary>
		void SetIsDeflatedReference(bool value);

		/// <summary>
		/// Sets the primary key(s)
		/// </summary>
		void SetPrimaryKeys(PropertyInfoAndValue[] primaryKeys);

		/// <summary>
		/// Sets the associated model of this object.
		/// </summary>
		void SetDataAccessModel(BaseDataAccessModel dataAccessModel);

		/// <summary>
		/// Sets the underlying data container of the current data access object with the one in the given domain object.
		/// </summary>
		void SwapData(IDataAccessObject source, bool transferChangedProperties);

		/// <summary>
		/// Sets the AutoIncrement key value of this object.
		/// </summary>
		void SetAutoIncrementKeyValue(object value);

		/// <summary>
		/// Returns true if the property with the given name has been changed since the object was loaded or created.
		/// </summary>
		bool HasPropertyChanged(string propertyName);

		object DataObject { get; }

		PropertyInfo[] GetIntegerAutoIncrementPropertyInfos();

		void SetIntegerAutoIncrementValues(object[] values);

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

		void Inflate();
	}
}
