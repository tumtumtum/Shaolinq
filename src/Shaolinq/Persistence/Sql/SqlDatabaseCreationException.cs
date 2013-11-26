// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

﻿using System;

namespace Shaolinq.Persistence.Sql
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
