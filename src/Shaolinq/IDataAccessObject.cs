// Copyright (c) 2007-2014 Thong Nguyen (tumtumtum@gmail.com)

 using System;
using System.Collections.Generic;
 using Shaolinq.Persistence;

namespace Shaolinq
{
	internal interface IDataAccessObjectInternal
	{
		/// <summary>
		/// Submits this object into the cache and then returns itself.
		/// </summary>
		/// <returns>The current object</returns>
		IDataAccessObject SubmitToCache();

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
		/// Sets the properties generated on the server side. The order of the values must be the same order
		/// as that returned by <see cref="IDataAccessObject.GetPropertiesGeneratedOnTheServerSide"/>
		/// </summary>
		/// <param name="values">An array of values to set. Must be in the same order as the properties returned 
		/// by <see cref="IDataAccessObject.GetPropertiesGeneratedOnTheServerSide"/>
		/// </param>
		void SetPropertiesGeneratedOnTheServerSide(object[] values);

		/// <summary>
		/// Update all properties that rely on server side generated properties.
		/// </summary>
		/// <returns></returns>
		bool ComputeServerGeneratedIdDependentComputedTextProperties();

		/// <summary>
		/// Resets the modified status of all the properties that aren't unrealised
		/// foreign key references.
		/// </summary>
		IDataAccessObject ResetModified();

		/// <summary>
		/// Sets the underlying data container of the current data access object with the one in the given domain object.
		/// </summary>
		void SwapData(IDataAccessObject source, bool transferChangedProperties);
	}

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
		DataAccessModel DataAccessModel { get; }

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
		/// Returns the number of direct properties on this object that make up the primary key.
		/// </summary>
		int NumberOfPrimaryKeys { get; }

		/// <summary>
		/// Returns the number of direct properties on this object that make up the primary key
		/// and are generated on the server side.
		/// </summary>
		int NumberOfPrimaryKeysGeneratedOnServerSide { get; }

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
		IDataAccessObject ResetModified();

		/// <summary>
		/// Called when an object has finished being loaded from the database
		/// </summary>
		/// <returns></returns>
		IDataAccessObject FinishedInitializing();

		/// <summary>
		/// Returns True if this object defines any direct properties that are generated on
		/// the server side.
		/// </summary>
		bool DefinesAnyDirectPropertiesGeneratedOnTheServerSide { get; }

		/// <summary>
		/// Returns True if the object is missing any auto increment integer primary keys.
		/// This check includes primary keys that are made up of objects with primary keys 
		/// </summary>
		bool IsMissingAnyDirectOrIndirectServerSideGeneratedPrimaryKeys { get; }

		/// <summary>
		/// Returns the number of direct properties generated on the server side.
		/// </summary>
		int NumberOfDirectPropertiesGeneratedOnTheServerSide { get; }

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
		/// Sets the primary keys.
		/// </summary>
		void SetPrimaryKeys(ObjectPropertyValue[] primaryKeys);

		/// <summary>
		/// Sets the underlying data container of the current data access object with the one in the given domain object.
		/// </summary>
		void SwapData(IDataAccessObject source, bool transferChangedProperties);

		/// <summary>
		/// Returns true if the property with the given name has been changed since the object was loaded or created.
		/// </summary>
		bool HasPropertyChanged(string propertyName);

		/// <summary>
		/// Sets the properties generated on the server side. The order of the values must be the same order
		/// as that returned by <see cref="GetPropertiesGeneratedOnTheServerSide"/>
		/// </summary>
		/// <param name="values">An array of values to set. Must be in the same order as the properties returned 
		/// by <see cref="GetPropertiesGeneratedOnTheServerSide"/>
		/// </param>
		void SetPropertiesGeneratedOnTheServerSide(object[] values);

		/// <summary>
		/// Gets an array of the primary keys and their values.
		/// This property is generated using Reflection.Emit.  Strings inside the returned <see cref="ObjectPropertyValue"/>
		/// are guaranteed to be interned.
		/// </summary>
		ObjectPropertyValue[] GetPrimaryKeys();

		/// <summary>
		/// Gets an array of the primary keys and their values.
		/// This property is generated using Reflection.Emit.  Strings inside the returned <see cref="ObjectPropertyValue"/>
		/// are guaranteed to be interned.
		/// </summary>
		ObjectPropertyValue[] GetPrimaryKeysFlattened();

		/// <summary>
		/// Gets an array of the primary keys and their values.
		/// This property is generated using Reflection.Emit.  Strings inside the returned <see cref="ObjectPropertyValue"/>
		/// are guaranteed to be interned.
		/// </summary>
		ObjectPropertyValue[] GetPrimaryKeysForUpdateFlattened();

		/// <summary>
		/// Gets an array of properties generated on the server side. Does not return values.
		/// </summary>
		ObjectPropertyValue[] GetPropertiesGeneratedOnTheServerSide();

		/// <summary>
		/// Gets an array of all the properties on this object
		/// </summary>
		ObjectPropertyValue[] GetAllProperties();

		/// <summary>
		/// Gets an array of all the properties that are DataAccessObjects on this object
		/// </summary>
		ObjectPropertyValue[] GetRelatedObjectProperties();

		/// <summary>
		/// Gets a list of all the properties on this object that have changed since the object was loaded or created.
		/// This property is generated using Reflection.Emit.  Strings inside the returned <see cref="ObjectPropertyValue"/>
		/// are guaranteed to be interned.
		/// </summary>
		List<ObjectPropertyValue> GetChangedProperties();

		/// <summary>
		/// Gets a list of all the properties on this object that have changed since the object was loaded or created.
		/// This property is generated using Reflection.Emit.  Strings inside the returned <see cref="ObjectPropertyValue"/>
		/// are guaranteed to be interned. Properties that are DataAccessObjects will be returned as individual primary key
		/// properties.
		/// </summary>
		List<ObjectPropertyValue> GetChangedPropertiesFlattened();

		/// <summary>
		/// Update all properties that rely on server side generated properties.
		/// </summary>
		/// <returns></returns>
		bool ComputeServerGeneratedIdDependentComputedTextProperties();

		/// <summary>
		/// Inflates the current object if the object is currently deflated.  A deflated object only contains
		/// primary keys and no other property values. Inflation usually requires a database query.
		/// </summary>
		DataAccessObject Inflate();

		/// <summary>
		/// Returns True if the primary keys are ready to be submitted to the underlying RDBMS.
		/// A primary key is ready if all of its component primary key properties are either
		/// set or autoincrement (will be generated by the server). This means server generated properties
		/// could be emptyy.
		/// </summary>
		bool PrimaryKeyIsCommitReady { get; }

		/// <summary>
		/// Submits this object into the cache and then returns itself.
		/// </summary>
		/// <returns>The current object</returns>
		DataAccessObject SubmitToCache();
	}
}
