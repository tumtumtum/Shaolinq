// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System;

namespace Shaolinq.Persistence
{
	public abstract class SqlDataTypeProvider
	{
		public ConstraintDefaultsConfiguration ConstraintDefaultsConfiguration { get; }

		protected SqlDataTypeProvider(ConstraintDefaultsConfiguration constraintDefaultsConfiguration)
		{
			this.ConstraintDefaultsConfiguration = constraintDefaultsConfiguration;
		}

		public virtual Type GetTypeForEnums()
		{
			return typeof(string);
		}

		public abstract SqlDataType GetSqlDataType(Type type);
	}
}
