// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

﻿using System;

namespace Shaolinq
{
	public class ConcurrencyException
		: DataAccessException
	{
		public ConcurrencyException()
		{
		}

		public ConcurrencyException(string message, string relatedQuery)
			: base(message, relatedQuery)
		{
		}

		public ConcurrencyException(Exception innerException, string relatedQuery)
			: base(innerException, relatedQuery)
		{	
		}
	}
}
