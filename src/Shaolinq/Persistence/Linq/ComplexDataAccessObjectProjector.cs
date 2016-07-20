// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Data;
using System.Linq;

namespace Shaolinq.Persistence.Linq
{
	/// <summary>
	/// Projects DataAccessObjects that are contained inside non-DAO objects
	/// </summary>
	public class ComplexDataAccessObjectProjector<T, U>
		: ObjectProjector<T, U, ComplexDataAccessObjectProjector<T, U>.Context>
		where U : T
	{
		#region Context
		public class Context
		{
			private T value;
			internal object[] keys;
			internal bool HasValue { get; set; }
			internal T Value { get { return this.value; } set { this.value = value; this.HasValue = true; } }

			internal Context()
			{
			}
		}
		#endregion

		private readonly Func<IDataReader, object[]> keysGenerator;

		public ComplexDataAccessObjectProjector(SqlQueryProvider provider, DataAccessModel dataAccessModel, SqlDatabaseContext sqlDatabaseContext, SqlQueryFormatResult formatResult, object[] placeholderValues, Func<ObjectProjector, IDataReader, int, object[], Func<DataAccessObject, DataAccessObject>, U> objectReader, Func<IDataReader, object[]> keysGenerator)
			: base(provider, dataAccessModel, sqlDatabaseContext, formatResult, placeholderValues, objectReader)
		{
			this.keysGenerator = keysGenerator;
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

			lastValue = default(T);

			return false;
		}

		protected internal override bool ProcessMoveNext(IDataReader dataReader, T value, ref Context context, out T result)
		{
			var currentKeys = this.keysGenerator(dataReader);

			if (!context.HasValue || currentKeys.SequenceEqual(context.keys))
			{
				if (!context.HasValue)
				{
					context.keys = currentKeys;
				}
				
				result = value;
				context.Value = value;
				
				return false;
			}

			result = context.Value;
			context.Value = value;
			context.keys = currentKeys;

			return true;
		}
	}
}