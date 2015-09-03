// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

﻿using System;

namespace Shaolinq.Persistence
{
	public class SqlDatabaseCreationException
		: Exception
	{
		public SqlDatabaseCreationException(string message)
			: base(message)
		{
		}
	}
}
