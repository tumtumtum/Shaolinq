namespace Shaolinq
{
	public class DataAccessModelHookRollbackContext : DataAccessModelHookContextBase
	{
		public DataAccessModelHookRollbackContext(TransactionContext transactionContext) : base(transactionContext)
		{
		}
	}
}