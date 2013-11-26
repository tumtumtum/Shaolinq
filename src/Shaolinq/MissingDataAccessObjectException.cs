// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

﻿namespace Shaolinq
{
	/// <summary>
	/// Thrown when a trying to update a DAO that cannot be found. Usually occurs
	/// when trying to commit an update to a DAO that has been deleted by another transaction
	/// or when trying to commit an update to a delated DAO with an invalid primary key.
	/// </summary>
	public class MissingDataAccessObjectException
		: InvalidDataAccessObjectAccessException
	{
	}
}
