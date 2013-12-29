// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

﻿using System;
using System.Data;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Platform;
using Platform.Text;

namespace Shaolinq.Persistence
{
	public class DefaultListSqlDataType<T>
		: DefaultStringSqlDataType
	{
		public DefaultListSqlDataType(Type supportedType)
			: base(supportedType)
		{
		}

		public override Pair<Type, object> ConvertForSql(object value)
		{
			if (value == null)
			{
				return new Pair<Type, object>(typeof(string), "");
			}

			return new Pair<Type, object>(typeof(string), ListToString(((ShaolinqList<T>)value)));
		}

		private const string StringsToEscape = "%|+;";

		public static string ListToString(ShaolinqList<T> list)
		{
			var builder = new StringBuilder();

			foreach (var value in list)
			{
				builder.Append(TextConversion.ToEscapedHexString(Convert.ToString(value), StringsToEscape));
				builder.Append(';');
			}

			if (builder.Length > 0)
			{
				builder.Length--;
			}

			return builder.ToString();
		}

		private static readonly MethodInfo ReaderToListMethod = typeof(DefaultListSqlDataType<T>).GetMethod("ReaderToList");

		public static ShaolinqList<T> ReaderToList(IDataReader dataReader, int ordinal)
		{
			if (dataReader.IsDBNull(ordinal))
			{
				return new ShaolinqList<T>();
			}
			else
			{
				var retval = new ShaolinqList<T>();
				var stringValue = dataReader.GetString(ordinal);
                
				if (stringValue.Length == 0)
				{
					return retval;
				}

				foreach (string s in stringValue.Split(';'))
				{
					var value = TypeHelper.ConvertValue<T>(TextConversion.FromEscapedHexString(s));

					retval.Add(value);
				}

				retval.Changed = false;

				return retval;
			}
		}

		public override Expression GetReadExpression(ParameterExpression objectProjector, ParameterExpression dataReader, int ordinal)
		{
			return Expression.Call(null, ReaderToListMethod, dataReader, Expression.Constant(ordinal));
		}
	}
}
