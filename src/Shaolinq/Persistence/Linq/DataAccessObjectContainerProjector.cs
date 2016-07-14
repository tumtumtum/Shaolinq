using System;
using System.Data;
using System.Linq;

namespace Shaolinq.Persistence.Linq
{
	public class DataAccessObjectContainerProjector<T, U>
		: ObjectProjector<T, U>
		where U : T
	{
		private class Optional
		{
			private T value;
			public object[] keys;
			public bool HasValue { get; private set; }
			public T Value { get { return this.value; } set { this.value = value; this.HasValue = true; } }
		}

		private readonly Func<IDataReader, object[]> keysGenerator;

		public DataAccessObjectContainerProjector(SqlQueryProvider provider, DataAccessModel dataAccessModel, SqlDatabaseContext sqlDatabaseContext, SqlQueryFormatResult formatResult, object[] placeholderValues, Func<ObjectProjector, IDataReader, int, object[], Func<DataAccessObject, DataAccessObject>, U> objectReader, Func<IDataReader, object[]> keysGenerator)
			: base(provider, dataAccessModel, sqlDatabaseContext, formatResult, placeholderValues, objectReader)
		{
			this.keysGenerator = keysGenerator;
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

			lastValue = default(T);

			return false;
		}

		protected internal override bool ProcessMoveNext(IDataReader dataReader, T value, ref object context, out T result)
		{
			var optional = (Optional)context;
			var currentKeys = this.keysGenerator(dataReader);

			if (!optional.HasValue || currentKeys.SequenceEqual(optional.keys))
			{
				if (!optional.HasValue)
				{
					optional.keys = currentKeys;
				}
				
				result = value;
				optional.Value = value;
				
				return false;
			}

			result = optional.Value;
			optional.Value = value;
			optional.keys = currentKeys;

			return true;
		}
	}
}