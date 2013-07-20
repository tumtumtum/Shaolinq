namespace Shaolinq.Persistence.Sql.DevartPostgres
{
	public class DevartPostgresSqlDatabaseCreator
		: SqlDatabaseCreator
	{
		public DevartPostgresSqlDatabaseCreator(SqlPersistenceContext sqlPersistenceContext, BaseDataAccessModel model, DataAccessModelPersistenceContextInfo persistenceContextInfo)
			: base(sqlPersistenceContext, model, persistenceContextInfo)
		{
		}
	}
}