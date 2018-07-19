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
		public string CommandText { get; }
		public IReadOnlyList<TypedValue> ParameterValues { get; }

		public ObjectProjector(SqlQueryProvider queryProvider, DataAccessModel dataAccessModel, SqlDatabaseContext sqlDatabaseContext, string commandText, IReadOnlyList<TypedValue> parameterValues)
		{
			this.QueryProvider = queryProvider;
			this.DataAccessModel = dataAccessModel;
			this.SqlDatabaseContext = sqlDatabaseContext;
			this.CommandText = commandText;
			this.ParameterValues = parameterValues;
		}
	}

	public class ObjectProjector<T>
		: ObjectProjector<T, object>
	{
		public ObjectProjector(SqlQueryProvider queryProvider, DataAccessModel dataAccessModel, SqlDatabaseContext sqlDatabaseContext, string commandText, IReadOnlyList<TypedValue> parameterValues, object[] placeholderValues, ObjectReaderFunc<T> objectReader)
			: base(queryProvider, dataAccessModel, sqlDatabaseContext, commandText, parameterValues, placeholderValues, objectReader)
		{
		}
	}

	public class ObjectProjector<T, C>
		: ObjectProjector, IAsyncEnumerable<T>
		where C : class
	{
		protected internal readonly object[] placeholderValues;
		protected internal readonly ObjectReaderFunc<T> objectReader;

		public ObjectProjector(SqlQueryProvider queryProvider, DataAccessModel dataAccessModel, SqlDatabaseContext sqlDatabaseContext, string commandText, IReadOnlyList<TypedValue> parameterValues, object[] placeholderValues, ObjectReaderFunc<T> objectReader)
			: base(queryProvider, dataAccessModel, sqlDatabaseContext, commandText, parameterValues)
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

		IEnumerator IEnumerable.GetEnumerator() => this.GetAsyncEnumerator();
		public virtual IEnumerator<T> GetEnumerator() => this.GetAsyncEnumerator();
		public virtual IAsyncEnumerator<T> GetAsyncEnumerator() => new ObjectProjectionAsyncEnumerator<T, C>(this);
	}
}
