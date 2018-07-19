// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Data;

namespace Shaolinq.Persistence.Linq
{
	public class AlwaysReadFirstObjectProjector<T, U>
		: ObjectProjector<T, U>
		where U : T
	{
		public AlwaysReadFirstObjectProjector(SqlQueryProvider queryProvider, DataAccessModel dataAccessModel, SqlDatabaseContext sqlDatabaseContext, SqlQueryFormatResult formatResult, object[] placeholderValues, ObjectReaderFunc<U> objectReader)
			: base(queryProvider, dataAccessModel, sqlDatabaseContext, formatResult, placeholderValues, objectReader)
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