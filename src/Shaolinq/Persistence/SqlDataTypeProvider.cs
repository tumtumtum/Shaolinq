// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

﻿using System;

namespace Shaolinq.Persistence
{
	public abstract class SqlDataTypeProvider
	{
		public ConstraintDefaults ConstraintDefaults { get; private set; }

		protected SqlDataTypeProvider(ConstraintDefaults constraintDefaults)
		{
			this.ConstraintDefaults = constraintDefaults;
		}

		public virtual Type GetTypeForEnums()
		{
			return typeof(string);
		}

		public abstract SqlDataType GetSqlDataType(Type type);
	}
}
