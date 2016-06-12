// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System.Data;
using System.Data.Common;

namespace Shaolinq.Persistence
{
	public static partial class DataReaderExtensions
	{
		public static T Cast<T>(this IDataRecord reader)
			where T : class, IDataRecord
		{
			return (reader as T) ?? (T)((reader as MarsDataReader))?.Inner;
		}

		[RewriteAsync]
		public static bool ReadEx(this IDataReader reader)
		{
			var dbDataReader = reader as DbDataReader;

			if (dbDataReader != null)
			{
				return dbDataReader.Read();
			}

			return reader.Read();
		}
	}
}
