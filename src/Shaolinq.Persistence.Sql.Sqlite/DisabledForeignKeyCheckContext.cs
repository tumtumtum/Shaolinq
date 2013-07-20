namespace Shaolinq.Persistence.Sql.Sqlite
{
	internal class DisabledForeignKeyCheckContext
		: IDisabledForeignKeyCheckContext
	{
		public DisabledForeignKeyCheckContext(PersistenceTransactionContext context)
		{
		}

		public virtual void Dispose()
		{
		}
	}
}
