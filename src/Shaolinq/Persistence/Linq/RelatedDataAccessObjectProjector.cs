// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Linq;
using Shaolinq.Persistence.Linq.Expressions;

namespace Shaolinq.Persistence.Linq
{
	public class RelatedDataAccessObjectProjector<T, U>
		: ObjectProjector<T, U>
		where U : T
		where T : DataAccessObject
	{
		public RelatedDataAccessObjectProjector(IQueryProvider provider, DataAccessModel dataAccessModel, SqlQueryFormatResult formatResult, SqlDatabaseContext sqlDatabaseContext, Delegate objectReader, IRelatedDataAccessObjectContext relatedDataAccessObjectContext, SelectFirstType selectFirstType, SqlAggregateType? sqlAggregateType, bool isDefaultIfEmpty, object[] placeholderValues)
			: base(provider, dataAccessModel, formatResult, sqlDatabaseContext, objectReader, relatedDataAccessObjectContext, selectFirstType, sqlAggregateType, isDefaultIfEmpty, placeholderValues)
		{
		}

		public override IEnumerator<T> GetEnumerator()
		{
			var transactionContext = this.DataAccessModel.GetCurrentContext(false);

			using (var acquisition = transactionContext.AcquirePersistenceTransactionContext(this.SqlDatabaseContext))
			{
				var transactionalCommandsContext = acquisition.SqlDatabaseCommandsContext;

				using (var dataReader = transactionalCommandsContext.ExecuteReader(this.FormatResult.CommandText, this.FormatResult.ParameterValues))
				{
					while (dataReader.Read())
					{
						T retval = this.objectReader(this, dataReader, this.placeholderValues);

						this.relatedDataAccessObjectContext.InitializeDataAccessObject?.Invoke(this.relatedDataAccessObjectContext.RelatedDataAccessObject, (IDataAccessObjectAdvanced)retval);

						yield return retval;

						this.count++;
					}
				}
			}
		}
	}
}
