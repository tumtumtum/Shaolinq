// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

﻿using System;

namespace Shaolinq.Persistence
{
	public abstract class SqlDataTypeProvider
	{
		public abstract SqlDataType GetSqlDataType(Type type);
	}
}
