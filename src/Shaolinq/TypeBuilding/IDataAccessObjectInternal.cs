// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)
namespace Shaolinq.TypeBuilding
{
	public interface IDataAccessObjectInternal
	{
		object CompositePrimaryKey { get; }

		/// <summary>
		/// Submits this object into the cache and then returns itself.
		/// </summary>
		/// <returns>The current object</returns>
		IDataAccessObjectInternal SubmitToCache();

		/// <summary>
		/// Called when an object has finished being loaded from the database
		/// </summary>
		/// <returns></returns>
		IDataAccessObjectInternal FinishedInitializing();

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

		void MarkServerSidePropertiesAsApplied();

		/// <summary>
		/// Update all properties that rely on server side generated properties.
		/// </summary>
		/// <returns></returns>
		bool ComputeServerGeneratedIdDependentComputedTextProperties();

		/// <summary>
		/// Resets the modified status of all the properties that aren't unrealised
		/// foreign key references.
		/// </summary>
		IDataAccessObjectInternal ResetModified();

		/// <summary>
		/// Sets the primary keys.
		/// </summary>
		void SetPrimaryKeys(ObjectPropertyValue[] primaryKeys);

		bool HasAnyChangedPrimaryKeyServerSideProperties { get; }

		/// <summary>
		/// Sets the underlying data container of the current data access object with the one in the given domain object.
		/// </summary>
		void SwapData(DataAccessObject source, bool transferChangedProperties);

		int GetHashCodeAccountForServerGenerated();

		bool EqualsAccountForServerGenerated(object dataAccessObject);
	}
}