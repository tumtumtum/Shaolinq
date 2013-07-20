using System;

namespace Shaolinq
{
	/// <summary>
	/// Represents the state of the current object within the current transaction.
	/// </summary>
	[Flags]
	public enum ObjectState
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
		NewChanged = 2 | Changed,

		/// <summary>
		/// The object is missing some contrained foreign keys and cannot be
		/// persisted until those foreign keys have been realised.
		/// </summary>
		MissingForeignKeys = 4,

		/// <summary>
		/// The object is missing some unconstrained foreign keys and may be
		/// persisted and will need to be updated with the foreign keys once
		/// they are realised.
		/// </summary>
		MissingUnconstrainedForeignKeys = 8,

		/// <summary>
		/// The object has been deleted
		/// </summary>
		Deleted = 16
	}
}
