// Copyright (c) 2007-2014 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Linq.Expressions;
using System.Transactions;
using Shaolinq.Persistence.Linq;
using log4net;

namespace Shaolinq.Persistence
{
	public abstract class SqlDatabaseSchemaManager
		: IDisposable
	{
		public SqlDatabaseContext SqlDatabaseContext { get; private set; }
		public ServerSqlDataDefinitionExpressionBuilder ServerSqlDataDefinitionExpressionBuilder { get; private set; } 
		private static readonly ILog Log = LogManager.GetLogger(typeof(SqlDatabaseSchemaManager).Name);

		protected SqlDatabaseSchemaManager(SqlDatabaseContext sqlDatabaseContext)
		{
			this.SqlDatabaseContext = sqlDatabaseContext;
			this.ServerSqlDataDefinitionExpressionBuilder = new ServerSqlDataDefinitionExpressionBuilder(this);
		}

		public virtual void CreateDatabaseAndSchema(bool overwrite)
		{
			if (!this.CreateDatabaseOnly(overwrite))
			{
				return;
			}

			this.CreateDatabaseSchema();
		}

		protected virtual SqlDataDefinitionBuilderFlags GetBuilderFlags()
		{
			return SqlDataDefinitionBuilderFlags.BuildTables | SqlDataDefinitionBuilderFlags.BuildIndexes;
		}

		protected abstract bool CreateDatabaseOnly(bool overwrite);

		protected virtual Expression BuildDataDefinitonExpressions()
		{
			return SqlDataDefinitionExpressionBuilder.Build(this.SqlDatabaseContext.SqlDataTypeProvider, this.SqlDatabaseContext.SqlDialect, this.SqlDatabaseContext.DataAccessModel, this.SqlDatabaseContext.TableNamePrefix, this.GetBuilderFlags());
		}

		protected virtual void CreateDatabaseSchema()
		{
			using (var scope = new TransactionScope(TransactionScopeOption.RequiresNew))
			{
				using (var dataTransactionContext = this.SqlDatabaseContext.CreateSqlTransactionalCommandsContext(null))
				{
					using (this.SqlDatabaseContext.AcquireDisabledForeignKeyCheckContext(dataTransactionContext))
					{
						var dataDefinitionExpressions = this.BuildDataDefinitonExpressions();

						var result = this.SqlDatabaseContext.SqlQueryFormatterManager.Format(dataDefinitionExpressions);

						using (var command = dataTransactionContext.CreateCommand(SqlCreateCommandOptions.Default | SqlCreateCommandOptions.UnpreparedExecute))
						{
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

		public virtual void Dispose()
		{
		}
	}
}
