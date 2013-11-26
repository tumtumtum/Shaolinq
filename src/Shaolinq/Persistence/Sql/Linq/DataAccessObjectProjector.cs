// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace Shaolinq.Persistence.Sql.Linq
{
	public class DataAccessObjectProjector<T, U>
		: ObjectProjector<T, U>
		where U : T
		where T : IDataAccessObject
	{
		public DataAccessObjectProjector(IQueryProvider provider, BaseDataAccessModel dataAccessModel, SqlQueryFormatResult formatResult, PersistenceContext persistenceContextStore, Delegate objectReader, IRelatedDataAccessObjectContext relatedDataAccessObjectContext, SelectFirstType selectFirstType, object[] placeholderValues)
			: base(provider, dataAccessModel, formatResult, persistenceContextStore, objectReader, relatedDataAccessObjectContext, selectFirstType, placeholderValues)
		{
		}

		public override IEnumerator<T> GetEnumerator()
		{
			var transactionContext = this.DataAccessModel.AmbientTransactionManager.GetCurrentContext(false);

			using (var acquisition = transactionContext.AcquirePersistenceTransactionContext(this.PersistenceContext))
			{
				var persistenceTransactionContext = (SqlPersistenceTransactionContext)acquisition.PersistenceTransactionContext;

				using (this.dataReader = persistenceTransactionContext.ExecuteReader(this.FormatResult.CommandText, this.FormatResult.ParameterValues))
				{
					while (dataReader.Read())
					{
						if (count == 1 && this.selectFirstType == SelectFirstType.SingleOrDefault || this.selectFirstType == SelectFirstType.DefaultIfEmpty)
						{
							throw new InvalidOperationException("Sequence contains more than one element");
						}

						T retval = this.objectReader(this, this.dataReader, this.placeholderValues);

						retval.ResetModified();

						yield return (T)this.DataAccessModel.GetCurrentDataContext(false).CacheObject(retval, false);

						count++;
					}
				}
			}
		}
	}
}
