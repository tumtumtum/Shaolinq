using System;
using System.Data;

namespace Shaolinq.Persistence.Linq
{
	public class DataAccessObjectContainerProjector<T, U>
		: ObjectProjector<T, U>
		where U : T
	{
		private class Optional
		{
			private T value;
			public bool HasValue { get; private set; }
			public T Value { get { return this.value; } set { this.value = value; this.HasValue = true; } }
		}

		private readonly Func<T, T, bool> outputComparer;

		public DataAccessObjectContainerProjector(SqlQueryProvider provider, DataAccessModel dataAccessModel, SqlDatabaseContext sqlDatabaseContext, SqlQueryFormatResult formatResult, object[] placeholderValues, Func<ObjectProjector, IDataReader, int, object[], Func<DataAccessObject, DataAccessObject>, U> objectReader)
			: base(provider, dataAccessModel, sqlDatabaseContext, formatResult, placeholderValues, objectReader)
		{
			outputComparer = DataAccessObjectAwareResultTypeComparerBuilder.CreateComparer<T>();
		}

		protected internal override object CreateEnumerationContext(IDataReader dataReader, int executionVersion)
		{
			return new Optional();
		}

		protected internal override bool ProcessLastMoveNext(ref object context, out T lastValue)
		{
			var optional = (Optional)context;

			if (optional.HasValue)
			{
				lastValue = optional.Value;

				return true;
			}

			lastValue = default(T);

			return false;
		}

		protected internal override bool ProcessMoveNext(T value, ref object context, out T result)
		{
			var optional = (Optional)context;

			if (!optional.HasValue || outputComparer(value, optional.Value))
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