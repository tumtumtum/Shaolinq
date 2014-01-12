using System.Transactions;
using Shaolinq.Persistence.Linq;
using log4net;

namespace Shaolinq.Persistence
{
	public abstract class SqlDatabaseSchemaManager
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(SqlDatabaseSchemaManager).Name);

		protected readonly SqlDatabaseContext sqlDatabaseContext;
	
		protected SqlDatabaseSchemaManager(SqlDatabaseContext sqlDatabaseContext)
		{
			this.sqlDatabaseContext = sqlDatabaseContext;
		}

		public virtual void CreateDatabaseAndSchema(bool overwrite)
		{
			if (!this.CreateDatabaseOnly(overwrite))
			{
				return;
			}

			this.CreateDatabaseSchema();
		}

		protected abstract bool CreateDatabaseOnly(bool overwrite);
		
		protected virtual void CreateDatabaseSchema()
		{
			using (var scope = new TransactionScope(TransactionScopeOption.RequiresNew))
			{
				using (var dataTransactionContext = this.sqlDatabaseContext.CreateDatabaseTransactionContext(null))
				{
					using (sqlDatabaseContext.AcquireDisabledForeignKeyCheckContext(dataTransactionContext))
					{
						var dataDefinitionExpressions = SqlDataDefinitionExpressionBuilder.Build(sqlDatabaseContext.SqlDataTypeProvider, sqlDatabaseContext.SqlDialect, this.sqlDatabaseContext.DataAccessModel, sqlDatabaseContext.TableNamePrefix);

						var result = sqlDatabaseContext.SqlQueryFormatterManager.Format(dataDefinitionExpressions);

						using (var command = ((DefaultSqlDatabaseTransactionContext)dataTransactionContext).CreateCommand(SqlCreateCommandOptions.Default | SqlCreateCommandOptions.UnpreparedExecute))
						{
							command.Transaction = null;
							command.CommandText = result.CommandText;

							if (Log.IsDebugEnabled)
							{
								Log.Debug(command.CommandText);
							}

							command.ExecuteNonQuery();
						}
					}
				}

				scope.Complete();
			}
		}
	}
}
