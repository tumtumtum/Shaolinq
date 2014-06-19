// Copyright (c) 2007-2014 Thong Nguyen (tumtumtum@gmail.com)

﻿using System;

namespace Shaolinq
{
	/// <summary>
	/// An exception that is thrown when a type extending <see cref="DataAccessObject{OBJECT_TYPE}"/> is expected.
	/// </summary>
	public class ExpectedDataAccessObjectTypeException
		: Exception
	{
		public Type Type { get; private set; }

		public ExpectedDataAccessObjectTypeException(Type type)
			: base(String.Concat("Expected a type extending DataAccessObject but got ", type))
		{
			this.Type = type;
		}
	}
}
