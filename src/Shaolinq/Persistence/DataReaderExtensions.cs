// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Data;
using System.Data.Common;

namespace Shaolinq.Persistence
{
	public static partial class DataReaderExtensions
	{
		public static T Cast<T>(this IDataRecord reader)
			where T : class, IDataRecord
		{
			return reader as T ?? (T)(reader as MarsDataReader)?.Inner;
		}
		
		[RewriteAsync]
		public static bool ReadEx(this IDataReader reader)
		{
			var dbDataReader = reader as DbDataReader;

			return dbDataReader?.Read() ?? reader.Read();
		}

		[RewriteAsync]
		public static bool NextResultEx(this IDataReader reader)
		{
			var dbDataReader = reader as DbDataReader;

			return dbDataReader?.NextResult() ?? reader.NextResult();
		}

		[RewriteAsync]
		public static bool IsDbNullEx(this IDataReader reader, int ordinal)
		{
			var dbDataReader = reader as DbDataReader;

			return dbDataReader?.IsDBNull(ordinal) ?? reader.IsDBNull(ordinal);
		}

		[RewriteAsync]
		public static T GetFieldValueEx<T>(this IDataReader reader, int ordinal)
		{

			if (reader is DbDataReader dbDataReader)
			{
				return dbDataReader.GetFieldValue<T>(ordinal);
			}

			return (T)Convert.ChangeType(reader.GetValue(ordinal), typeof(T));
		}
	}
}
