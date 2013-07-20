namespace Shaolinq.Persistence.Sql.Postgres
{
	public class PostgresSqlDatabaseCreator
		: SqlDatabaseCreator
	{
		public PostgresSqlDatabaseCreator(SqlPersistenceContext sqlPersistenceContext, BaseDataAccessModel model, DataAccessModelPersistenceContextInfo persistenceContextInfo)
			: base(sqlPersistenceContext, model, persistenceContextInfo)
		{
		}
	}
}