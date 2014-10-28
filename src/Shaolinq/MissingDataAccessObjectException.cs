// Copyright (c) 2007-2014 Thong Nguyen (tumtumtum@gmail.com)

using System;

namespace Shaolinq
{
	/// <summary>
	/// Thrown when trying to update an object that does not exist or when updating
	/// related property to an object that does not exist.
	/// </summary>
	public class MissingDataAccessObjectException
		: InvalidDataAccessObjectAccessException
	{
		/// <summary>
		/// The object that is missing (if known). Can be null if constraints are deferred.
		/// </summary>
		public IDataAccessObjectAdvanced MissingObject { get; private set; }

		public MissingDataAccessObjectException()
			: this(null, null, null)
		{
		}

		public MissingDataAccessObjectException(Exception innerException, string relatedQuery)
			: this(null, innerException, relatedQuery)
		{	
		}

		public MissingDataAccessObjectException(IDataAccessObjectAdvanced missingObject)
			: this(missingObject, null, null)
		{	
		}

		public MissingDataAccessObjectException(IDataAccessObjectAdvanced missingObject, Exception innerException, string relatedQuery)
			: base(innerException, relatedQuery)
		{
			this.MissingObject = missingObject;
		}
	}
}
