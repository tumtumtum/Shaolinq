using System.Transactions;
using Shaolinq.Persistence.Sql;
using Shaolinq.Persistence.Sql.Linq;
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
			var connection = this.model.GetCurrentDatabaseConnection(DatabaseReadMode.ReadWrite);

			using (var scope = new TransactionScope(TransactionScopeOption.Suppress))
			{
				using (var dataTransactionContext = connection.NewDataTransactionContext(this.model, null))
				{
					using (connection.AcquireDisabledForeignKeyCheckContext(dataTransactionContext))
					{
						var dataDefinitionExpressions = SqlDataDefinitionExpressionBuilder.Build(connection.SqlDataTypeProvider, connection.SqlDialect, this.model);

						var formatter = connection.NewQueryFormatter(this.model, connection.SqlDataTypeProvider, connection.SqlDialect, dataDefinitionExpressions, SqlQueryFormatterOptions.Default);

						var result = formatter.Format();


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
