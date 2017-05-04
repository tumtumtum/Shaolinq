// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Reflection;
using Shaolinq.Persistence;

namespace Shaolinq.Sqlite
{
	public class SqliteBooleanType
		: PrimitiveSqlDataType
	{
		public SqliteBooleanType(ConstraintDefaultsConfiguration constraintDefaultsConfiguration, Type type, string sqlName, MethodInfo getMethod)
			: base(constraintDefaultsConfiguration, type, sqlName, getMethod)
		{
		}

		public override TypedValue ConvertForSql(object value)
		{
			return new TypedValue(typeof(bool), null, c => Convert.ToBoolean(c) ? 1 : 0).ChangeValue(value);
		}
	}
}