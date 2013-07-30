﻿using System.Collections.Generic;
using System.Transactions;

namespace Shaolinq
{
	public static class TransactionScopeExtensions
	{
		public static void Flush(this TransactionScope scope, BaseDataAccessModel dataAccessModel)
		{
			dataAccessModel.FlushCurrentTransaction();
		}

		public static T Import<T>(this TransactionScope scope, T dataAccessObject)
			where T : IDataAccessObject
		{
			dataAccessObject.DataAccessModel.GetCurrentDataContext(true).ImportObject(dataAccessObject);

			return dataAccessObject;
		}

		public static void Import<T>(this TransactionScope scope, params T[] dataAccessObjects)
			where T : IDataAccessObject
		{
			foreach (var dataAccessObject in dataAccessObjects)
			{
				dataAccessObject.DataAccessModel.GetCurrentDataContext(true).ImportObject(dataAccessObject);
			}
		}

		public static void Import<T>(this TransactionScope scope, IEnumerable<T> dataAccessObjects)
			where T : IDataAccessObject
		{
			foreach (var dataAccessObject in dataAccessObjects)
			{
				dataAccessObject.DataAccessModel.GetCurrentDataContext(true).ImportObject(dataAccessObject);
			}
		}
	}
}