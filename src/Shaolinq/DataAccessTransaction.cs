// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Transactions;

namespace Shaolinq
{
	public class DataAccessTransaction
	{
		private static readonly AsyncLocal<DataAccessTransaction> current = new AsyncLocal<DataAccessTransaction>();

		public static DataAccessTransaction Current
		{
			get
			{
				var value = current.Value;

				if (value != null)
				{
					if (value.SystemTransaction == Transaction.Current)
					{
						return value;
					}

					current.Value = null;
				}

				return null;
			}

			internal set
			{
				current.Value = value;
			}
		}

		internal Transaction SystemTransaction { get; set; }
		internal bool HasSystemTransaction => this.SystemTransaction != null;
		internal Dictionary<TransactionContext, DataAccessModel> dataAccessModelsByTransactionContext = new Dictionary<TransactionContext, DataAccessModel>();
		private bool aborted;

		public DataAccessIsolationLevel IsolationLevel { get; set; } = DataAccessIsolationLevel.Unspecified;
		public bool HasAborted => aborted || this.SystemTransaction?.TransactionInformation.Status == TransactionStatus.Aborted;
		public IEnumerable<DataAccessModel> ParticipatingDataAccessModels => this.dataAccessModelsByTransactionContext.Values;

		public DataAccessTransaction()
		{
			this.SystemTransaction = Transaction.Current;
		}

		public void Abort()
		{
			this.aborted = true;
		}

		public void AddTransactionContext(TransactionContext context)
		{
			this.dataAccessModelsByTransactionContext[context] = context.dataAccessModel;

			this.SystemTransaction?.EnlistVolatile(context, EnlistmentOptions.None);
		}
	}
}