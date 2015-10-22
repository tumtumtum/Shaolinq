using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Platform;
using Shaolinq.Postgres.Shared;

namespace Shaolinq.Postgres
{
	public class PostgresTimespanSqlDataType
		: PostgresSharedTimespanSqlDataType
	{
		public PostgresTimespanSqlDataType(ConstraintDefaults constraintDefaults, Type supportedType) : base(constraintDefaults, supportedType)
		{
		}

		public override Pair<Type, object> ConvertForSql(object value)
		{
			if (this.UnderlyingType != null)
			{
				return new Pair<Type, object>(this.UnderlyingType, value);
			}
			else
			{
				return new Pair<Type, object>(this.SupportedType, value);
			}
		}
    }
}
