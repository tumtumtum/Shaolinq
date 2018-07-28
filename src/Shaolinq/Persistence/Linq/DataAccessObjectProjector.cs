// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)

using System.Collections.Generic;
using System.Data;

namespace Shaolinq.Persistence.Linq
{
	public class DataAccessObjectProjector<T>
		: ObjectProjector<T, DataAccessObjectProjector<T>.Context>
		where T : DataAccessObject
	{
		#region Context
		public class Context
		{
			private T value;
			internal bool HasValue { get; private set; }
			internal T Value { get => this.value; set { this.value = value; this.HasValue = true; } }

			internal Context()
			{
			}
		}
		#endregion

		public DataAccessObjectProjector(SqlQueryProvider provider, DataAccessModel dataAccessModel, SqlDatabaseContext sqlDatabaseContext, string commandText, IReadOnlyList<TypedValue> parameterValues, object[] placeholderValues, ObjectReaderFunc<T> objectReader)
			: base(provider, dataAccessModel, sqlDatabaseContext, commandText, parameterValues, placeholderValues, objectReader)
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
