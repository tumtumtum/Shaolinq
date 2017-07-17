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

		/// <summary>
		/// Import the given <see cref="DataAccessObject"/> into the current scope.
		/// </summary>
		/// <typeparam name="T">The type of <see cref="DataAccessObject"/> to import</typeparam>
		/// <param name="dataAccessObject">The <see cref="DataAccessObject"/> to import</param>
		/// <returns>The given <see cref="DataAccessObject"/> unless a cached copy of the <see cref="DataAccessObject"/>
		/// already exists in the current context in which case the existing instance is merged with the imported object
		/// and the existing instance returned.</returns>
		/// <remarks>
		/// <para>Use this method to import a <see cref="DataAccessObject"/> stored from a different context into the
		/// current context.</para>
		/// <para>Each <see cref="DataAccessScope"/> has a context that contains a cache of all the <see cref="DataAccessObject"/>s that have been
		/// created or queried. Subsequent queries will return the same instance of the object. Importing an object that is
		/// already cached will not replace the existing object with the imported object but rather changes from the imported
		/// object will be applied to the existing object in a merge operation. For the purposes of a merge, changes in the imported
		/// object have higher priority than uncommited changes in the existing object in the cache.</para>
		/// </remarks>
		public static T Import<T>(this TransactionScope scope, T dataAccessObject)
			where T : DataAccessObject
		{
			var context = dataAccessObject.GetDataAccessModel().GetCurrentDataContext(true);

			if (context == null)
			{
				throw new InvalidOperationException("No current DataAccessContext");
			}
			
			context.ImportObject(dataAccessObject);

			return dataAccessObject;
		}

		/// <summary>
		/// Import the given <see cref="DataAccessObject"/>s into the current scope.
		/// </summary>
		/// <typeparam name="T">The type of <see cref="DataAccessObject"/>s to import</typeparam>
		/// <param name="dataAccessObjects">The <see cref="DataAccessObject"/>s to import</param>
		/// <remarks>
		/// <para>Use this method to import a set of <see cref="DataAccessObject"/>s stored from a different context into the
		/// current context.</para>
		/// <para>Each <see cref="DataAccessScope"/> has a context that contains a cache of all the <see cref="DataAccessObject"/>s that have been
		/// created or queried. Subsequent queries will return the same instance of the object. Importing an object that is
		/// already cached will not replace the existing object with the imported object but rather changes from the imported
		/// object will be applied to the existing object in a merge operation. For the purposes of a merge, changes in the imported
		/// object have higher priority than uncommited changes in the existing object in the cache.</para>
		/// </remarks>
		public static void Import<T>(this TransactionScope scope, params T[] dataAccessObjects)
			where T : DataAccessObject
		{
			foreach (var dataAccessObject in dataAccessObjects)
			{
				var context = dataAccessObject.GetDataAccessModel().GetCurrentDataContext(true);

				if (context == null)
				{
					throw new InvalidOperationException("No current DataAccessContext");
				}
				
				context.ImportObject(dataAccessObject);
			}
		}

		/// <summary>
		/// Import the given <see cref="DataAccessObject"/>s into the current scope.
		/// </summary>
		/// <typeparam name="T">The type of <see cref="DataAccessObject"/>s to import</typeparam>
		/// <param name="dataAccessObjects">The <see cref="DataAccessObject"/>s to import</param>
		/// <remarks>
		/// <para>Use this method to import a set of <see cref="DataAccessObject"/>s stored from a different context into the
		/// current context.</para>
		/// <para>Each <see cref="DataAccessScope"/> has a context that contains a cache of all the <see cref="DataAccessObject"/>s that have been
		/// created or queried. Subsequent queries will return the same instance of the object. Importing an object that is
		/// already cached will not replace the existing object with the imported object but rather changes from the imported
		/// object will be applied to the existing object in a merge operation. For the purposes of a merge, changes in the imported
		/// object have higher priority than uncommited changes in the existing object in the cache.</para>
		/// </remarks>
		public static void Import<T>(this TransactionScope scope, IEnumerable<T> dataAccessObjects)
			where T : DataAccessObject
		{
			foreach (var dataAccessObject in dataAccessObjects)
			{
				var context = dataAccessObject.GetDataAccessModel().GetCurrentDataContext(true);

				if (context == null)
				{
					throw new InvalidOperationException("No current DataAccessContext");
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
