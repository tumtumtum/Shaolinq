// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;

namespace Shaolinq.Persistence.Linq
{
	public class ObjectProjector
	{
		public DataAccessModel DataAccessModel { get; }
		public SqlDatabaseContext SqlDatabaseContext { get; }
		
		public SqlQueryProvider QueryProvider { get; }
		protected internal readonly IRelatedDataAccessObjectContext relatedDataAccessObjectContext;
        protected internal readonly SqlQueryFormatResult formatResult;

        public ObjectProjector(SqlQueryProvider queryProvider, DataAccessModel dataAccessModel, SqlDatabaseContext sqlDatabaseContext, IRelatedDataAccessObjectContext relatedDataAccessObjectContext, SqlQueryFormatResult formatResult)
		{
			this.QueryProvider = queryProvider;
			this.DataAccessModel = dataAccessModel;
			this.SqlDatabaseContext = sqlDatabaseContext;
			this.relatedDataAccessObjectContext = relatedDataAccessObjectContext;
            this.formatResult = formatResult;
		}
	}
	
	public class ObjectProjector<T, U>
		: ObjectProjector, IEnumerable<T>, IAsyncEnumerable<T>
		where U : T
	{
		protected internal readonly object[] placeholderValues;
		protected internal readonly Func<ObjectProjector, IDataReader, int, object[], U> objectReader;
		
		public ObjectProjector(SqlQueryProvider queryProvider, DataAccessModel dataAccessModel, SqlDatabaseContext sqlDatabaseContext, IRelatedDataAccessObjectContext relatedDataAccessObjectContext, SqlQueryFormatResult formatResult, object[] placeholderValues, Func<ObjectProjector, IDataReader, int, object[], U> objectReader)
			: base(queryProvider, dataAccessModel, sqlDatabaseContext, relatedDataAccessObjectContext, formatResult)
		{
			this.placeholderValues = placeholderValues;
			this.objectReader = objectReader;
		}

	    protected internal virtual bool ProcessLastMoveNext(ref object context, out T result)
	    {
            result = default(T);

            return false;
	    }
        
        protected internal virtual bool ProcessMoveNext(T value, ref object context, out T result)
        {
            result = value;

            return true;
        }

	    protected internal virtual object CreateEnumerationContext()
	    {
            return null;
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
					    var enumerationContext = this.CreateEnumerationContext();

					    while (dataReader.Read())
					    {
                            T result;
					        var current = objectReader(this, dataReader, versionContext.Version, placeholderValues);

                            if (this.ProcessMoveNext(current, ref enumerationContext, out result))
					        {
					            yield return result;
					        }
					    }

					    T lastResult;

					    if (this.ProcessLastMoveNext(ref enumerationContext, out lastResult))
					    {
					        yield return lastResult;
					    }
					}
				}
			}
		}
        
	    IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
	    public virtual IAsyncEnumerator<T> GetAsyncEnumerator() => new AsyncEnumerator<T, U>(this);
	}
}
