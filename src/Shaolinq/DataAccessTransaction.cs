// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

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

		internal bool aborted;
		internal Transaction SystemTransaction { get; set; }
		internal bool HasSystemTransaction => this.SystemTransaction != null;
		internal Dictionary<TransactionContext, DataAccessModel> dataAccessModelsByTransactionContext = new Dictionary<TransactionContext, DataAccessModel>();
		
		public DataAccessIsolationLevel IsolationLevel { get; set; } = DataAccessIsolationLevel.Unspecified;
		public bool HasAborted => this.aborted|| this.SystemTransaction?.TransactionInformation.Status == TransactionStatus.Aborted;

		public IEnumerable<DataAccessModel> ParticipatingDataAccessModels => this.dataAccessModelsByTransactionContext.Values;

		public DataAccessTransaction()
		{
			this.SystemTransaction = Transaction.Current;
		}

		public void AddTransactionContext(TransactionContext context)
		{
			this.dataAccessModelsByTransactionContext[context] = context.dataAccessModel;

			this.SystemTransaction?.EnlistVolatile(context, EnlistmentOptions.None);
		}
	}
}