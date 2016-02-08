// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Transactions;

namespace Shaolinq
{
	public class DataAccessTransactionAbortedException
		: Exception
	{
	}

	public class DataAccessScope
		: IDisposable
	{
		DataAccessIsolationLevel IsolationLevel { get; set; }

		private bool complete;
		private readonly DataAccessTransaction transaction;

		public DataAccessScope()
		{	
		}

		public DataAccessScope(DataAccessIsolationLevel isolationLevel)
		{
			this.IsolationLevel = isolationLevel;

			if (DataAccessTransaction.Current == null)
			{
				this.transaction = new DataAccessTransaction();

				DataAccessTransaction.Current = transaction;
			}
		}

		public void Complete()
		{
			this.complete = true;
		}

		public void Dispose()
		{
			if (!complete)
			{
				throw new DataAccessTransactionAbortedException();
			}

			if (this.transaction != null && this.transaction.SystemTransaction == null)
			{
				foreach (var dataAccessModel in this.transaction.dataAccessModels)
				{
					dataAccessModel.asyncLocalTransactionContext.Value.Commit();
				}
			}
		}
	}

	public class DataAccessTransaction
	{
		private static readonly AsyncLocal<DataAccessTransaction> current = new AsyncLocal<DataAccessTransaction>();

		public static DataAccessTransaction Current
		{
			get
			{
				if (current.Value != null)
				{
					return current.Value;
				}

				if (Transaction.Current == null)
				{
					return null;
				}

				current.Value = new DataAccessTransaction();

				return current.Value;
			}

			internal set
			{
				current.Value = value;
			}
		}

		internal HashSet<DataAccessModel> dataAccessModels = new HashSet<DataAccessModel>();

		internal Transaction SystemTransaction { get; set; }
		public DataAccessIsolationLevel IsolationLevel { get; set; } = DataAccessIsolationLevel.Unspecified;
		public bool HasAborted => this.SystemTransaction.TransactionInformation.Status == TransactionStatus.Aborted;

		public DataAccessTransaction()
		{
			this.SystemTransaction = Transaction.Current;
		}
	}
}