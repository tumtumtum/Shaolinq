// Copyright (c) 2007-2014 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using Shaolinq.Persistence;

namespace Shaolinq
{
	/// <summary>
	/// An interface implemented by all data access objects.
	/// </summary>
	public interface IDataAccessObjectAdvanced
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
		/// Returns the Primary Key type for this object.  If the object defines a composite key the return value will be null
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
		/// Returns True if this object defines any direct properties that are generated on
		/// the server side.
		/// </summary>
		bool DefinesAnyDirectPropertiesGeneratedOnTheServerSide { get; }

		/// <summary>
		/// Returns True if the object is missing any auto increment integer primary keys.
		/// This check includes primary keys that are made up of objects with primary keys 
		/// </summary>
		bool IsMissingAnyDirectOrIndirectServerSideGeneratedPrimaryKeys { get; }

		bool IsMissingAnyPrimaryKeys { get; }

		/// <summary>
		/// Returns the number of direct properties generated on the server side.
		/// </summary>
		int NumberOfPropertiesGeneratedOnTheServerSide { get; }

		/// <summary>
		/// Returns true if the current object doesn't belong to any transaction (is freeform)
		/// </summary>
		bool IsTransient { get; }

		/// <summary>
		/// Deletes the object.
		/// </summary>
		void Delete();

		/// <summary>
		/// Returns true if the property with the given name has been changed since the object was loaded or created.
		/// </summary>
		bool HasPropertyChanged(string propertyName);

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
	}
}
