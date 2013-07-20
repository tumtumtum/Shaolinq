namespace Shaolinq.Persistence.Sql.Postgres.Shared
{
	public class DisabledForeignKeyCheckContext
		: IDisabledForeignKeyCheckContext
	{
		public DisabledForeignKeyCheckContext(PersistenceTransactionContext context)
		{
			var command = ((SqlPersistenceTransactionContext)context).DbConnection.CreateCommand();

			command.CommandText = "SET CONSTRAINTS ALL DEFERRED;";
			command.ExecuteNonQuery();
		}

		public virtual void Dispose()
		{
		}
	}
}
