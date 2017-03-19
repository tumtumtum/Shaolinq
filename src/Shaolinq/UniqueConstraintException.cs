// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System;

namespace Shaolinq
{
	/// <summary>
	/// An object with the same primary key exists or an object with a unique property 
	/// with the same value exists.
	/// </summary>
	public class UniqueConstraintException
		: DataAccessException
	{
		public UniqueConstraintException(Exception innerException, string relatedQuery)
			: base(innerException, relatedQuery)
		{	
		}
	}
}
