// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Linq.Expressions;
using System.Reflection;
using Platform;
using Shaolinq.Persistence;

namespace Shaolinq.SqlServer
{
	public class SqlServerDecimalDataType
		: SqlDataType
	{
		private readonly MethodInfo method;
		public string SqlName { get; }
		
		public SqlServerDecimalDataType(ConstraintDefaultsConfiguration constraintDefaultsConfiguration, Type type, string sqlName)
			: base(constraintDefaultsConfiguration, type)
		{
			this.SqlName = sqlName;
			this.method = this.GetType().GetMethod("Read", BindingFlags.Public | BindingFlags.Static);
		}

		public override string GetSqlName(PropertyDescriptor propertyDescriptor)
		{
			return this.SqlName;
		}

		public static decimal Read(IDataRecord record, int ordinal)
		{
			var reader = (SqlDataReader) record;

			return ToDecimal(reader.GetSqlDecimal(ordinal));
		}

		private static Decimal ToDecimal(SqlDecimal value)
		{
			var data = value.Data;
			var scale = value.Scale;

			if (data[3] != 0 || scale > 28)
			{
				var result = decimal.Parse(value.ToString());

				return result;
			}

			return new Decimal(data[0], data[1], data[2], !value.IsPositive, scale);
		}

		public override Expression GetReadExpression(Expression dataReader, int ordinal)
		{
			return Expression.Condition
			(
				Expression.Call(dataReader, IsDbNullMethod, Expression.Constant(ordinal)),
				Expression.Convert(Expression.Constant(this.SupportedType.GetDefaultValue()), this.SupportedType),
				Expression.Convert(Expression.Call(null, this.method, dataReader, Expression.Constant(ordinal)), this.SupportedType)
			);
		}
	}
}
