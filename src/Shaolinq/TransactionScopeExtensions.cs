// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Transactions;
using Shaolinq.Persistence;

namespace Shaolinq
{
	public static partial class TransactionScopeExtensions
	{
		[RewriteAsync]
		public static void Save(this TransactionScope scope)
		{
			if (DataAccessTransaction.Current == null)
			{
				return;
			}

			foreach (var dataAccessModel in DataAccessTransaction.Current.ParticipatingDataAccessModels)
			{
				if (!dataAccessModel.IsDisposed)
				{
					dataAccessModel.Flush();
				}
			}
		}

		[RewriteAsync]
		public static void Save(this TransactionScope scope, DataAccessModel dataAccessModel)
		{
			if (!dataAccessModel.IsDisposed)
			{
				dataAccessModel.Flush();
			}
		}

		[RewriteAsync]
		public static void Flush(this TransactionScope scope)
		{
			scope.Save();
		}

		[RewriteAsync]
		public static void Flush(this TransactionScope scope, DataAccessModel dataAccessModel)
		{
			scope.Save(dataAccessModel);
		}

		public static void SetReadOnly(this TransactionScope scope, DataAccessModel dataAccessModel)
		{
			dataAccessModel.SetCurentTransactionReadOnly();
		}

		public static T Import<T>(this TransactionScope scope, T dataAccessObject)
			where T : DataAccessObject
		{
			var context = dataAccessObject.GetDataAccessModel().GetCurrentDataContext(true);

		    if (context == null)
		    {
		        throw new InvalidOperationException("No Current DataAccessContext");
		    }
            
			context.ImportObject(dataAccessObject);

			return dataAccessObject;
		}

		public static void Import<T>(this TransactionScope scope, params T[] dataAccessObjects)
			where T : DataAccessObject
		{
            foreach (var dataAccessObject in dataAccessObjects)
			{
                var context = dataAccessObject.GetDataAccessModel().GetCurrentDataContext(true);

                if (context == null)
                {
                    throw new InvalidOperationException("No Current DataAccessContext");
                }
                
                context.ImportObject(dataAccessObject);
			}
		}

		public static void Import<T>(this TransactionScope scope, IEnumerable<T> dataAccessObjects)
			where T : DataAccessObject
		{
			foreach (var dataAccessObject in dataAccessObjects)
			{
                var context = dataAccessObject.GetDataAccessModel().GetCurrentDataContext(true);

                if (context == null)
                {
                    throw new InvalidOperationException("No Current DataAccessContext");
                }

                dataAccessObject.GetDataAccessModel().GetCurrentDataContext(true).ImportObject(dataAccessObject);
			}
		}

        /// <summary>
        /// Retrieves the current <see cref="SqlTransactionalCommandsContext"/> for direct access to the database.
        /// </summary>
        /// <param name="scope">The current scope</param>
        /// <param name="model">The dataaccess model</param>
        /// <returns>The <see cref="SqlTransactionalCommandsContext"/></returns>
		public static SqlTransactionalCommandsContext GetCurrentSqlTransactionalCommandsContext(this TransactionScope scope, DataAccessModel model)
		{
			return model.GetCurrentCommandsContext();
		}
	}
}
