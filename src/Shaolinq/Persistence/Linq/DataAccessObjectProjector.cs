// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Shaolinq.Persistence.Linq
{
	public class DataAccessObjectProjector<T, U>
		: ObjectProjector<T, U>
		where U : T
		where T : DataAccessObject
	{
		public DataAccessObjectProjector(SqlQueryProvider provider, DataAccessModel dataAccessModel, SqlDatabaseContext sqlDatabaseContext, IRelatedDataAccessObjectContext relatedDataAccessObjectContext, SqlQueryFormatResult formatResult, object[] placeholderValues, Func<ObjectProjector, IDataReader, int, object[], U> objectReader)
			: base(provider, dataAccessModel, sqlDatabaseContext, relatedDataAccessObjectContext, formatResult, placeholderValues, objectReader)
		{
		}

		public override IEnumerator<T> GetEnumerator()
		{
			var transactionContext = this.DataAccessModel.GetCurrentContext(false);

			using (var versionContext = transactionContext.AcquireVersionContext())
			{
				using (var acquisition = transactionContext.AcquirePersistenceTransactionContext(this.SqlDatabaseContext))
				{
					var persistenceTransactionContext = acquisition.SqlDatabaseCommandsContext;

					using (var dataReader = persistenceTransactionContext.ExecuteReader(formatResult.CommandText, formatResult.ParameterValues))
					{
						T previous = null;

						while (dataReader.Read())
						{
							var current = this.objectReader(this, dataReader, versionContext.Version, this.placeholderValues);

							if (previous == null || current == previous)
							{
								previous = current;

								continue;
							}

							previous.ToObjectInternal().ResetModified();

							yield return previous;

							previous = current;
						}

						if (previous != null)
						{
							yield return previous;
						}
					}
				}
			}
		}
	}
}
