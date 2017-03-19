// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System;

namespace Shaolinq
{
	/// <summary>
	/// Represents the state of the current object within the current transaction.
	/// </summary>
	[Flags]
	public enum DataAccessObjectState
	{
		/// <summary>
		/// The object is unchanged.
		/// </summary>
		Unchanged = 0,

		/// <summary>
		/// The object has changed.
		/// </summary>
		Changed = 1,

		/// <summary>
		/// The object is new.
		/// </summary>
		New = 2,

		/// <summary>
		/// The object is new and has changed.
		/// </summary>
		NewChanged = New | Changed,

		/// <summary>
		/// The object has just been commited.
		/// </summary>
		ServerSidePropertiesHydrated = 4,
		
		/// <summary>
		/// The object references a new object
		/// </summary>
		ReferencesNewObject = 8,

		/// <summary>
		/// The object's primary key references a new object
		/// </summary>
		PrimaryKeyReferencesNewObject = 16 | ReferencesNewObject,

		/// <summary>
		/// The object references an object that has server side properties that aren't yet known
		/// </summary>
		ReferencesNewObjectWithServerSideProperties = ReferencesNewObject | 32,

		/// <summary>
		/// The object's primary key references an object that has server side properties that aren't yet known
		/// </summary>
		PrimaryKeyReferencesNewObjectWithServerSideProperties = PrimaryKeyReferencesNewObject | 64,
		
		/// <summary>
		/// The object is a deflated reference where the primary key is known
		/// </summary>
		Deflated = 2048,

		/// <summary>
		/// The object is a deflated reference where keys other than the primary key are known
		/// </summary>
		DeflatedPredicated = Deflated | 4096,

		/// <summary>
		/// The object was manually constructed and cannot be used for persistence
		/// </summary>
		Untracked = 8192,
		
		/// <summary>
		/// The object has been deleted
		/// </summary>
		Deleted = 16384
	}
}
