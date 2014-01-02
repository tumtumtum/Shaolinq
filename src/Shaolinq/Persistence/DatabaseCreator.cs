using System.Transactions;
using Shaolinq.Persistence.Linq;
using log4net;

namespace Shaolinq.Persistence
{
	public abstract class DatabaseCreator
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(DatabaseCreator).Name);

		internal readonly DataAccessModel model;

		protected DatabaseCreator(DataAccessModel model)
		{
			this.model = model;
		}

		public virtual void Create(bool overwrite)
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
			var sqlDatabaseContext = this.model.GetCurrentSqlDatabaseContext();

			using (var scope = new TransactionScope(TransactionScopeOption.RequiresNew))
			{
				using (var dataTransactionContext = sqlDatabaseContext.NewDataTransactionContext(this.model, null))
				{
					using (sqlDatabaseContext.AcquireDisabledForeignKeyCheckContext(dataTransactionContext))
					{
						var dataDefinitionExpressions = SqlDataDefinitionExpressionBuilder.Build(sqlDatabaseContext.SqlDataTypeProvider, sqlDatabaseContext.SqlDialect, this.model, sqlDatabaseContext.TableNamePrefix);

						var result = sqlDatabaseContext.SqlQueryFormatterManager.Format(dataDefinitionExpressions);

						using (var command = ((SqlDatabaseTransactionContext)dataTransactionContext).CreateCommand(SqlCreateCommandOptions.Default | SqlCreateCommandOptions.UnpreparedExecute))
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
