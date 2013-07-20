namespace Shaolinq.Persistence.Sql.MySql
{
	public class MySqlSqlDatabaseCreator
		: SqlDatabaseCreator
	{
		public MySqlSqlDatabaseCreator(SqlPersistenceContext sqlPersistenceContext, BaseDataAccessModel model, DataAccessModelPersistenceContextInfo persistenceContextInfo)
			: base(sqlPersistenceContext, model, persistenceContextInfo)
		{
		}
	}
}