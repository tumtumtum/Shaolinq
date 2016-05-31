// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Data;

namespace Shaolinq.Persistence.Linq
{
	public class AlwaysReadFirstObjectProjector<T, U>
		: ObjectProjector<T, U>
		where U : T
	{
		public AlwaysReadFirstObjectProjector(SqlQueryProvider queryProvider, DataAccessModel dataAccessModel, SqlDatabaseContext sqlDatabaseContext, SqlQueryFormatResult formatResult, object[] placeholderValues, Func<ObjectProjector, IDataReader, int, object[], Func<DataAccessObject, DataAccessObject>, U> objectReader)
			: base(queryProvider, dataAccessModel, sqlDatabaseContext, formatResult, placeholderValues, objectReader)
		{
		}

		protected internal override object CreateEnumerationContext(IDataReader dataReader, int executionVersion)
		{
			return new Tuple<IDataReader, int>(dataReader, executionVersion);
		}

		protected internal override bool ProcessLastMoveNext(ref object context, out T result)
		{
			var contextInfo = (Tuple<IDataReader, int>)context;

			result = this.objectReader(this, contextInfo.Item1, contextInfo.Item2, this.placeholderValues, null);

			return true;
		}
	}
}