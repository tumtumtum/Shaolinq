// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System.Collections.Generic;
using System.Transactions;

namespace Shaolinq
{
	public static class TransactionScopeExtensions
	{
		public static void Flush(this TransactionScope scope, DataAccessModel dataAccessModel)
		{
			dataAccessModel.Flush();
		}

		public static void SetReadOnly(this TransactionScope scope, DataAccessModel dataAccessModel)
		{
			dataAccessModel.SetCurentTransactionReadOnly();
		}

		public static T Import<T>(this TransactionScope scope, T dataAccessObject)
			where T : DataAccessObject
		{
			dataAccessObject.GetDataAccessModel().GetCurrentDataContext(true).ImportObject(dataAccessObject);

			return dataAccessObject;
		}

		public static void Import<T>(this TransactionScope scope, params T[] dataAccessObjects)
			where T : DataAccessObject
		{
			foreach (var dataAccessObject in dataAccessObjects)
			{
				dataAccessObject.GetDataAccessModel().GetCurrentDataContext(true).ImportObject(dataAccessObject);
			}
		}

		public static void Import<T>(this TransactionScope scope, IEnumerable<T> dataAccessObjects)
			where T : DataAccessObject
		{
			foreach (var dataAccessObject in dataAccessObjects)
			{
				dataAccessObject.GetDataAccessModel().GetCurrentDataContext(true).ImportObject(dataAccessObject);
			}
		}

		public static SqlTransactionalCommandsContext GetCurrentSqlDataTransactionContext(this TransactionScope scope, DataAccessModel model)
		{
			return model.GetCurrentSqlDatabaseTransactionContext();
		}
	}
}
