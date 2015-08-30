// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System;

namespace Shaolinq
{
	/// <summary>
	/// Thrown when you try to use a deflated DAO reference to update an object but the deflated reference
	/// is invalid.
	/// </summary>
	public class InvalidDataAccessObjectAccessException
		: DataAccessException
	{
		public InvalidDataAccessObjectAccessException(Exception innerException, string relatedQuery)
			: base(innerException, relatedQuery)
		{	
		}
	}
}
