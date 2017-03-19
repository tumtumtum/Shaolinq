// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

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
		: ObjectProjector<T, U, object>
		where U : T
	{
		public ObjectProjector(SqlQueryProvider queryProvider, DataAccessModel dataAccessModel, SqlDatabaseContext sqlDatabaseContext, SqlQueryFormatResult formatResult, object[] placeholderValues, Func<ObjectProjector, IDataReader, int, object[], Func<DataAccessObject, DataAccessObject>, U> objectReader)
			: base(queryProvider, dataAccessModel, sqlDatabaseContext, formatResult, placeholderValues, objectReader)
		{
		}
	}

	public class ObjectProjector<T, U, C>
		: ObjectProjector, IAsyncEnumerable<T>
		where U : T
		where C : class
	{
		protected internal readonly object[] placeholderValues;
		protected internal readonly Func<ObjectProjector, IDataReader, int, object[], Func<DataAccessObject, DataAccessObject>, U> objectReader;

		public ObjectProjector(SqlQueryProvider queryProvider, DataAccessModel dataAccessModel, SqlDatabaseContext sqlDatabaseContext, SqlQueryFormatResult formatResult, object[] placeholderValues, Func<ObjectProjector, IDataReader, int, object[], Func<DataAccessObject, DataAccessObject>, U> objectReader)
			: base(queryProvider, dataAccessModel, sqlDatabaseContext, formatResult)
		{
			this.placeholderValues = placeholderValues;
			this.objectReader = objectReader;
		}

		protected internal virtual bool ProcessLastMoveNext(IDataReader dataReader, ref C context, out T result)
		{
			result = default(T);

			return false;
		}

		protected internal virtual bool ProcessMoveNext(IDataReader dataReader, T value, ref C context, out T result)
		{
			result = value;

			return true;
		}

		protected internal virtual C CreateEnumerationContext(IDataReader dataReader, int executionVersion)
		{
			return default(C);
		}

		protected internal virtual DataAccessObject ProcessDataAccessObject(DataAccessObject value, ref C context)
		{
			return value;	
		}

		IEnumerator IEnumerable.GetEnumerator() => this.GetAsyncEnumerator();
		public virtual IEnumerator<T> GetEnumerator() => this.GetAsyncEnumerator();
		public virtual IAsyncEnumerator<T> GetAsyncEnumerator() => new ObjectProjectionAsyncEnumerator<T, U, C>(this);
	}
}
