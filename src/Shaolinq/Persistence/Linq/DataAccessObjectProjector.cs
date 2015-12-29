// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Shaolinq.Persistence.Linq.Expressions;

namespace Shaolinq.Persistence.Linq
{
	public class DataAccessObjectProjector<T, U>
		: ObjectProjector<T, U>
		where U : T
		where T : DataAccessObject
	{
		public DataAccessObjectProjector(IQueryProvider provider, DataAccessModel dataAccessModel, SqlDatabaseContext sqlDatabaseContext, IRelatedDataAccessObjectContext relatedDataAccessObjectContext, ProjectorInfo projectorInfo, Func<ObjectProjector, IDataReader, object[], U> objectReader)
			: base(provider, dataAccessModel, sqlDatabaseContext, relatedDataAccessObjectContext, projectorInfo, objectReader)
		{
		}

		public override IEnumerator<T> GetEnumerator()
		{
			var transactionContext = this.DataAccessModel.GetCurrentContext(false);

			var info = this.projectorInfo;
			
			using (var acquisition = transactionContext.AcquirePersistenceTransactionContext(this.SqlDatabaseContext))
			{
				var persistenceTransactionContext = acquisition.SqlDatabaseCommandsContext;

				using (var dataReader = persistenceTransactionContext.ExecuteReader(info.FormatResult.CommandText, info.FormatResult.ParameterValues))
				{
					while (dataReader.Read())
					{
						T retval = objectReader(this, dataReader, info.PlaceholderValues);

						retval.ToObjectInternal().ResetModified();

						yield return retval;

						this.count++;
					}
				}
			}
		}
	}
}
