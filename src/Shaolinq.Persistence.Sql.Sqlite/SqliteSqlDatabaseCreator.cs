namespace Shaolinq.Persistence.Sql.Sqlite
{
	public class SqliteSqlDatabaseCreator
		: SqlDatabaseCreator
	{
		public SqliteSqlDatabaseCreator(SqlPersistenceContext sqlPersistenceContext, BaseDataAccessModel model, DataAccessModelPersistenceContextInfo persistenceContextInfo)
			: base(sqlPersistenceContext, model, persistenceContextInfo)
		{
		}
	}
}
