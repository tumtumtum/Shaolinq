// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Data;

namespace Shaolinq.Persistence.Linq
{
	public class RelatedDataAccessObjectProjector<T, U>
		: ObjectProjector<T, U>
		where U : T
		where T : DataAccessObject
	{
		public RelatedDataAccessObjectProjector(SqlQueryProvider queryProvider, DataAccessModel dataAccessModel, SqlDatabaseContext sqlDatabaseContext, IRelatedDataAccessObjectContext relatedDataAccessObjectContext, SqlQueryFormatResult formatResult, object[] placeholderValues, Func<ObjectProjector, IDataReader, int, object[], U> objectReader)
			: base(queryProvider, dataAccessModel, sqlDatabaseContext, relatedDataAccessObjectContext, formatResult, placeholderValues, objectReader)
		{
		}

		public override IEnumerator<T> GetEnumerator()
		{
			var transactionContext = this.DataAccessModel.GetCurrentContext(false);

			using (var versionContext = transactionContext.AcquireVersionContext())
			{
				using (var acquisition = transactionContext.AcquirePersistenceTransactionContext(this.SqlDatabaseContext))
				{
					var transactionalCommandsContext = acquisition.SqlDatabaseCommandsContext;

					using (var dataReader = transactionalCommandsContext.ExecuteReader(formatResult.CommandText, formatResult.ParameterValues))
					{
						while (dataReader.Read())
						{
							T retval = objectReader(this, dataReader, versionContext.Version, placeholderValues);

							this.relatedDataAccessObjectContext.InitializeDataAccessObject?.Invoke(this.relatedDataAccessObjectContext.RelatedDataAccessObject, (IDataAccessObjectAdvanced)retval);

							yield return retval;
						}
					}
				}
			}
		}
	}
}
