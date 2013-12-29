// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Linq;

namespace Shaolinq.Persistence.Linq
{
	public class RelatedDataAccessObjectProjector<T, U>
		: ObjectProjector<T, U>
		where U : T
		where T : IDataAccessObject
	{
		public RelatedDataAccessObjectProjector(IQueryProvider provider, DataAccessModel dataAccessModel, SqlQueryFormatResult formatResult, SqlDatabaseContext databaseConnection, Delegate objectReader, IRelatedDataAccessObjectContext relatedDataAccessObjectContext, SelectFirstType selectFirstType, object[] placeholderValues)
			: base(provider, dataAccessModel, formatResult, databaseConnection, objectReader, relatedDataAccessObjectContext, selectFirstType, placeholderValues)
		{
		}

		public override IEnumerator<T> GetEnumerator()
		{
			var transactionContext = this.DataAccessModel.AmbientTransactionManager.GetCurrentContext(false);

			using (var acquisition = transactionContext.AcquirePersistenceTransactionContext(this.DatabaseConnection))
			{
				var persistenceTransactionContext = (SqlDatabaseTransactionContext)acquisition.DatabaseTransactionContext;

				using (var dataReader = persistenceTransactionContext.ExecuteReader(this.FormatResult.CommandText, this.FormatResult.ParameterValues))
				{
					while (dataReader.Read())
					{
						if (count == 1 && this.selectFirstType == SelectFirstType.SingleOrDefault || this.selectFirstType == SelectFirstType.DefaultIfEmpty)
						{
							throw new InvalidOperationException("Sequence contains more than one element");
						}

						T retval = this.objectReader(this, dataReader, this.placeholderValues);

						if (this.relatedDataAccessObjectContext.InitializeDataAccessObject != null)
						{
							this.relatedDataAccessObjectContext.InitializeDataAccessObject(this.relatedDataAccessObjectContext.RelatedDataAccessObject, (IDataAccessObject)retval);
						}

						retval.ResetModified();

						yield return (T)this.DataAccessModel.GetCurrentDataContext(false).CacheObject(retval, false);

						count++;
					}
				}
			}
		}
	}
}
