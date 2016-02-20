// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System;
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
        
        protected internal override bool ProcessMoveNext(T value, ref object context, out T result)
        {
            this.relatedDataAccessObjectContext.InitializeDataAccessObject?.Invoke(this.relatedDataAccessObjectContext.RelatedDataAccessObject, value);

            result = value;

            return true;
        }
    }
}
