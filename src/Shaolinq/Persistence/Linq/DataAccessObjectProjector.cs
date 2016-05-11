// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Data;

namespace Shaolinq.Persistence.Linq
{
	public class DataAccessObjectProjector<T, U>
		: ObjectProjector<T, U>
		where U : T
		where T : DataAccessObject
	{
		public DataAccessObjectProjector(SqlQueryProvider provider, DataAccessModel dataAccessModel, SqlDatabaseContext sqlDatabaseContext, SqlQueryFormatResult formatResult, object[] placeholderValues, Func<ObjectProjector, IDataReader, int, object[], U> objectReader)
			: base(provider, dataAccessModel, sqlDatabaseContext, formatResult, placeholderValues, objectReader)
		{
		}

		protected internal override bool ProcessLastMoveNext(ref object context, out T lastValue)
		{
			if (context != null)
			{
				lastValue = (T)context;
				context = null;

				return true;
			}

			context = null;
			lastValue = null;

			return false;
		}

		protected internal override bool ProcessMoveNext(T value, ref object context, out T result)
		{
			if (context == default(T) || object.ReferenceEquals(value, context))
			{
				result = value;
				context = value;

				return false;
			}

			result = (T)context;
			result.ToObjectInternal().ResetModified();
			context = value;

			return true;
		}
	}
}
