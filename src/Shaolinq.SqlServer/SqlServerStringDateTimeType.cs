// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System;
using Shaolinq.Persistence;

namespace Shaolinq.SqlServer
{
	public class SqlServerStringSqlDataType
		: DefaultStringSqlDataType
	{
		public SqlServerStringSqlDataType(ConstraintDefaultsConfiguration constraintDefaultsConfiguration)
			: base(constraintDefaultsConfiguration)
		{
		}
		
		protected override string CreateVariableName(int maximumLength)
		{
			if (maximumLength == int.MaxValue)
			{
				return $"NVARCHAR(MAX)";
			}
			else
			{
				return $"NVARCHAR({maximumLength})";
			}
		}

		protected override string CreateFixedTypeName(int maximumLength)
		{
			if (maximumLength == int.MaxValue)
			{
				return $"NCHAR(MAX)";
			}
			else
			{
				return $"NCHAR({maximumLength})";
			}
		}

		protected override string CreateTextName()
		{
			return "NTEXT";
		}
	}
}
