namespace Shaolinq
{
	public abstract class DataAccessModelHookContextBase
	{
		protected readonly TransactionContext transactionContext;

		public string TransactionContextId => transactionContext?.TransactionContextId;
		public string DataAccessTransactionId => transactionContext?.DataAccessTransaction?.DataAccessTransactionId;

		internal DataAccessModelHookContextBase(TransactionContext transactionContext)
		{
			this.transactionContext = transactionContext;
		}
	}
}