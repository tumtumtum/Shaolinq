using System;
using Shaolinq.Persistence;

namespace Shaolinq.SqlServer
{
	public class SqlServerStringSqlDataType
		: DefaultStringSqlDataType
	{
		public SqlServerStringSqlDataType(ConstraintDefaults constraintDefaults)
			: base(constraintDefaults)
		{
		}

		public SqlServerStringSqlDataType(ConstraintDefaults constraintDefaults, Type type)
			: base(constraintDefaults, type)
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
