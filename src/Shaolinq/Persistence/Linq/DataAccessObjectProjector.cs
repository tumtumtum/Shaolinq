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
		private class Optional
		{
			private T value;
			public bool HasValue { get; private set; }
			public T Value { get { return value; } set { this.value = value; this.HasValue = true; } }
		}

		public DataAccessObjectProjector(SqlQueryProvider provider, DataAccessModel dataAccessModel, SqlDatabaseContext sqlDatabaseContext, SqlQueryFormatResult formatResult, object[] placeholderValues, Func<ObjectProjector, IDataReader, int, object[], Func<DataAccessObject, DataAccessObject>, U> objectReader)
			: base(provider, dataAccessModel, sqlDatabaseContext, formatResult, placeholderValues, objectReader)
		{
		}

		protected internal override object CreateEnumerationContext(IDataReader dataReader, int executionVersion)
		{
			return new Optional();
		}

		protected internal override bool ProcessLastMoveNext(IDataReader dataReader, ref object context, out T lastValue)
		{
			var optional = (Optional)context;

			if (optional.HasValue)
			{
				lastValue = optional.Value;
			
				return true;
			}
			
			lastValue = null;

			return false;
		}

		protected internal override bool ProcessMoveNext(IDataReader reader, T value, ref object context, out T result)
		{
			var optional = (Optional)context;

			if (!optional.HasValue || object.ReferenceEquals(value, optional.Value))
			{
				result = value;
				optional.Value = value;

				return false;
			}

			result = optional.Value;
			optional.Value = value;

			return true;
		}
	}
}
