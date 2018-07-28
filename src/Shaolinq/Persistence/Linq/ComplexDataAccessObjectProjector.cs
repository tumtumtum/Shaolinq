// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Shaolinq.Persistence.Linq
{
	/// <summary>
	/// Projects DataAccessObjects that are contained inside non-DAO objects
	/// </summary>
	public class ComplexDataAccessObjectProjector<T>
		: ObjectProjector<T, ComplexDataAccessObjectProjector<T>.Context>
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

		public ComplexDataAccessObjectProjector(SqlQueryProvider provider, DataAccessModel dataAccessModel, SqlDatabaseContext sqlDatabaseContext, string commandText, IReadOnlyList<TypedValue> parameterValues, object[] placeholderValues, ObjectReaderFunc<T> objectReader, Func<IDataReader, object[]> keysGenerator)
			: base(provider, dataAccessModel, sqlDatabaseContext, commandText, parameterValues, placeholderValues, objectReader)
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