// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using Shaolinq.Persistence.Linq.Expressions;

namespace Shaolinq.Persistence.Linq
{
	public class ObjectProjector
	{
		public DataAccessModel DataAccessModel { get; }
		public SqlDatabaseContext SqlDatabaseContext { get; }

		protected int count;
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
	
	/// <summary>
	/// Base class for ObjectReaders that use Reflection.Emit
	/// </summary>
	/// <typeparam name="T">
	/// The type of objects this projector returns
	/// </typeparam>
	/// <typeparam name="U">
	/// The concrete type for types this projector returns.  This type
	/// parameter is usually the same as <see cref="U"/> unless <see cref="T"/>
	/// is a <see cref="DataAccessObject{OBJECT_TYPE}"/> type in which case <see cref="U"/>
	/// must inherit from <see cref="T"/> and is usually automatically generated
	/// by the TypeBuilding system using Reflection.Emit.
	/// </typeparam>
	public class ObjectProjector<T, U>
		: ObjectProjector, IEnumerable<T>, IAsyncEumerable<T>
		where U : T
	{
		protected readonly object[] placeholderValues;
		protected readonly SqlQueryFormatResult formatResult;
		protected readonly Func<ObjectProjector, IDataReader, object[], U> objectReader;
		
		public ObjectProjector(SqlQueryProvider queryProvider, DataAccessModel dataAccessModel, SqlDatabaseContext sqlDatabaseContext, IRelatedDataAccessObjectContext relatedDataAccessObjectContext, SqlQueryFormatResult formatResult, object[] placeholderValues, Func<ObjectProjector, IDataReader, object[], U> objectReader)
			: base(queryProvider, dataAccessModel, sqlDatabaseContext, relatedDataAccessObjectContext)
		{
			this.formatResult = formatResult;
			this.placeholderValues = placeholderValues;
			this.objectReader = objectReader;
		}

		public virtual IEnumerator<T> GetEnumerator()
		{
			var transactionContext = this.DataAccessModel.GetCurrentContext(false);

			using (var acquisition = transactionContext.AcquirePersistenceTransactionContext(this.SqlDatabaseContext))
			{
				var transactionalCommandsContext = (DefaultSqlTransactionalCommandsContext)acquisition.SqlDatabaseCommandsContext;

				using (var dataReader = transactionalCommandsContext.ExecuteReader(formatResult.CommandText, formatResult.ParameterValues))
				{
					while (dataReader.Read())
					{
						yield return objectReader(this, dataReader, placeholderValues);

						this.count++;
					}
				}
			}
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return this.GetEnumerator();
		}

		public IAsyncEnumerator<T> GetAsyncEnumerator()
		{
			return new AsyncEnumeratorAdapter<T>(this.GetEnumerator());
		}
	}
}
