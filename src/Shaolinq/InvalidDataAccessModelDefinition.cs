// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

﻿using System;

namespace Shaolinq
{
	/// <summary>
	/// An exception that is thrown when there is a problem with the definition of a data access model and its associated data access object types.
	/// </summary>
	public class InvalidDataAccessObjectModelDefinition
		: Exception
	{
		public InvalidDataAccessObjectModelDefinition(string message)
			: base(message)
		{
		}

		public InvalidDataAccessObjectModelDefinition(string format, params object[] formatArgs)
			: base(String.Format(format, formatArgs))
		{
		}
	}
}
