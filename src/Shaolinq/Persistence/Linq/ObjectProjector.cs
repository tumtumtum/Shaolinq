// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Data;

namespace Shaolinq.Persistence.Linq
{
	public class ObjectProjector
	{
		public DataAccessModel DataAccessModel { get; }
		public SqlDatabaseContext SqlDatabaseContext { get; }
		
		public SqlQueryProvider QueryProvider { get; }
		protected readonly IRelatedDataAccessObjectContext relatedDataAccessObjectContext;

		public ObjectProjector(SqlQueryProvider queryProvider, DataAccessModel dataAccessModel, SqlDatabaseContext sqlDatabaseContext, IRelatedDataAccessObjectContext relatedDataAccessObjectContext)
		{
			this.QueryProvider = queryProvider;
			this.DataAccessModel = dataAccessModel;
			this.SqlDatabaseContext = sqlDatabaseContext;
			this.relatedDataAccessObjectContext = relatedDataAccessObjectContext;
		}
	}
	
	public class ObjectProjector<T, U>
		: ObjectProjector, IEnumerable<T>, IAsyncEumerable<T>
		where U : T
	{
		protected readonly object[] placeholderValues;
		protected readonly SqlQueryFormatResult formatResult;
		protected readonly Func<ObjectProjector, IDataReader, int, object[], U> objectReader;
		
		public ObjectProjector(SqlQueryProvider queryProvider, DataAccessModel dataAccessModel, SqlDatabaseContext sqlDatabaseContext, IRelatedDataAccessObjectContext relatedDataAccessObjectContext, SqlQueryFormatResult formatResult, object[] placeholderValues, Func<ObjectProjector, IDataReader, int, object[], U> objectReader)
			: base(queryProvider, dataAccessModel, sqlDatabaseContext, relatedDataAccessObjectContext)
		{
			this.formatResult = formatResult;
			this.placeholderValues = placeholderValues;
			this.objectReader = objectReader;
		}

		public virtual IEnumerator<T> GetEnumerator()
		{
			var transactionContext = this.DataAccessModel.GetCurrentContext(false);

			using (var versionContext = transactionContext.AcquireVersionContext())
			{
				using (var acquisition = transactionContext.AcquirePersistenceTransactionContext(this.SqlDatabaseContext))
				{
					var transactionalCommandsContext = (DefaultSqlTransactionalCommandsContext)acquisition.SqlDatabaseCommandsContext;

					using (var dataReader = transactionalCommandsContext.ExecuteReader(formatResult.CommandText, formatResult.ParameterValues))
					{
						while (dataReader.Read())
						{
							yield return objectReader(this, dataReader, versionContext.Version, placeholderValues);
						}
					}
				}
			}
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => this.GetEnumerator();
		public IAsyncEnumerator<T> GetAsyncEnumerator() => new AsyncEnumeratorAdapter<T>(this.GetEnumerator());
	}
}
