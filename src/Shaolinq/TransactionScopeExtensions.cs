// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System.Transactions;
using Shaolinq.Persistence;
using System.Collections.Generic;

namespace Shaolinq
{
	public static partial class TransactionScopeExtensions
	{
		[RewriteAsync]
		public static void Flush(this TransactionScope scope, DataAccessModel dataAccessModel)
		{
			dataAccessModel.Flush();
		}

		[RewriteAsync]
		public static void Flush(this TransactionScope scope)
		{
			foreach (var dataAccessModel in DataAccessTransaction.Current.ParticipatingDataAccessModels)
			{
				if (!dataAccessModel.IsDisposed)
				{
					dataAccessModel.Flush();
				}
			}
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
