// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Data;

namespace Shaolinq.Persistence.Linq
{
	public class AlwaysReadFirstObjectProjector<T>
		: ObjectProjector<T>
	{
		public AlwaysReadFirstObjectProjector(SqlQueryProvider queryProvider, DataAccessModel dataAccessModel, SqlDatabaseContext sqlDatabaseContext, string commandText, IReadOnlyList<TypedValue> parameterValues, object[] placeholderValues, ObjectReaderFunc<T> objectReader)
			: base(queryProvider, dataAccessModel, sqlDatabaseContext, commandText, parameterValues, placeholderValues, objectReader)
		{
		}

		protected internal override object CreateEnumerationContext(IDataReader dataReader, int executionVersion)
		{
			return new Tuple<IDataReader, int>(dataReader, executionVersion);
		}

		protected internal override bool ProcessLastMoveNext(IDataReader dataReader, ref object context, out T result)
		{
			var contextInfo = (Tuple<IDataReader, int>)context;

			result = this.objectReader(this, contextInfo.Item1, contextInfo.Item2, this.placeholderValues);

			return true;
		}
	}
}