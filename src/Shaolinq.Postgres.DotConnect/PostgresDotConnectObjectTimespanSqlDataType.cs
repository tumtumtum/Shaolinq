// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Platform;
using Shaolinq.Postgres.Shared;

namespace Shaolinq.Postgres.DotConnect
{
	public class PostgresDotConnectObjectTimespanSqlDataType
		: PostgresSharedTimespanSqlDataType
	{
		public PostgresDotConnectObjectTimespanSqlDataType(ConstraintDefaults constraintDefaults, Type supportedType)
			: base(constraintDefaults, supportedType)
		{
		}

		public override Pair<Type, object> ConvertForSql(object value)
		{
			return new Pair<Type, object>(this.SupportedType.GetUnwrappedNullableType(), value);
		}
	}
}
