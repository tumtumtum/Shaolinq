// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Data;

namespace Shaolinq.Persistence.Linq
{
	public class DataAccessObjectProjector<T, U>
		: ObjectProjector<T, U, DataAccessObjectProjector<T, U>.Context>
		where U : T
		where T : DataAccessObject
	{
		#region Context
		public class Context
		{
			private T value;
			internal bool HasValue { get; private set; }
			internal T Value { get { return this.value; } set { this.value = value; this.HasValue = true; } }

			internal Context()
			{
			}
		}
		#endregion

		public DataAccessObjectProjector(SqlQueryProvider provider, DataAccessModel dataAccessModel, SqlDatabaseContext sqlDatabaseContext, SqlQueryFormatResult formatResult, object[] placeholderValues, ObjectReaderFunc<U> objectReader)
			: base(provider, dataAccessModel, sqlDatabaseContext, formatResult, placeholderValues, objectReader)
		{
		}

		protected internal override Context CreateEnumerationContext(IDataReader dataReader, int executionVersion)
		{
			return new Context();
		}

		protected internal override bool ProcessLastMoveNext(IDataReader dataReader, ref Context context, out T lastValue)
		{
			if (context.HasValue)
			{
				lastValue = context.Value;
			
				return true;
			}
			
			lastValue = null;

			return false;
		}

		protected internal override bool ProcessMoveNext(IDataReader reader, T value, ref Context context, out T result)
		{
			if (!context.HasValue || ReferenceEquals(value, context.Value))
			{
				result = value;
				context.Value = value;

				return false;
			}

			result = context.Value;
			context.Value = value;

			return true;
		}
	}
}
