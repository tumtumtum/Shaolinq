// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;

namespace Shaolinq.Persistence.Linq
{
	public class ObjectProjector
	{
		public SqlQueryProvider QueryProvider { get; }
		public DataAccessModel DataAccessModel { get; }
		public SqlDatabaseContext SqlDatabaseContext { get; }
		protected internal readonly SqlQueryFormatResult formatResult;

		public ObjectProjector(SqlQueryProvider queryProvider, DataAccessModel dataAccessModel, SqlDatabaseContext sqlDatabaseContext, SqlQueryFormatResult formatResult)
		{
			this.QueryProvider = queryProvider;
			this.DataAccessModel = dataAccessModel;
			this.SqlDatabaseContext = sqlDatabaseContext;
			this.formatResult = formatResult;
		}
	}

	public class ObjectProjector<T, U>
		: ObjectProjector, IAsyncEnumerable<T>, IInternalAsyncEnumerable<T>
		where U : T
	{
		protected internal readonly object[] placeholderValues;
		protected internal readonly Func<ObjectProjector, IDataReader, int, object[], Func<DataAccessObject, DataAccessObject>, U> objectReader;

		public ObjectProjector(SqlQueryProvider queryProvider, DataAccessModel dataAccessModel, SqlDatabaseContext sqlDatabaseContext, SqlQueryFormatResult formatResult, object[] placeholderValues, Func<ObjectProjector, IDataReader, int, object[], Func<DataAccessObject, DataAccessObject>, U> objectReader)
			: base(queryProvider, dataAccessModel, sqlDatabaseContext, formatResult)
		{
			this.placeholderValues = placeholderValues;
			this.objectReader = objectReader;
		}

		protected internal virtual bool ProcessLastMoveNext(IDataReader dataReader, ref object context, out T result)
		{
			result = default(T);

			return false;
		}

		protected internal virtual bool ProcessMoveNext(IDataReader dataReader, T value, ref object context, out T result)
		{
			result = value;

			return true;
		}

		protected internal virtual object CreateEnumerationContext(IDataReader dataReader, int executionVersion)
		{
			return null;
		}

		protected internal virtual DataAccessObject ProcessDataAccessObject(DataAccessObject value, ref object context)
		{
			return value;	
		}

		IEnumerator IEnumerable.GetEnumerator() => this.GetAsyncEnumerator();
		public virtual IEnumerator<T> GetEnumerator() => this.GetAsyncEnumerator();
		public virtual IAsyncEnumerator<T> GetAsyncEnumerator() => new ObjectProjectionAsyncEnumerator<T, U>(this);
	}
}
