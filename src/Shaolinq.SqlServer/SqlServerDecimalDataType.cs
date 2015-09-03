// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Platform;
using Shaolinq.Persistence;

namespace Shaolinq.SqlServer
{
	public class SqlServerDecimalDataType
		: SqlDataType
	{
		private readonly MethodInfo method;
		public string SqlName { get; private set; }
		
		public SqlServerDecimalDataType(ConstraintDefaults constraintDefaults, Type type, string sqlName)
			: base(constraintDefaults, type)
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

		public override Expression GetReadExpression(ParameterExpression dataReader, int ordinal)
		{
			return Expression.Condition
			(
				Expression.Call(dataReader, IsDbNullMethod, Expression.Constant(ordinal)),
				Expression.Convert(Expression.Constant(this.SupportedType.GetDefaultValue()), this.SupportedType),
				Expression.Convert(Expression.Call(null, method, dataReader, Expression.Constant(ordinal)), this.SupportedType)
			);
		}
	}
}
