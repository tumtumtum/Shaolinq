// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Linq.Expressions;
using Shaolinq.Logging;
using Shaolinq.Persistence.Linq;

namespace Shaolinq.Persistence
{
	public abstract partial class SqlDatabaseSchemaManager
		: IDisposable
	{
		protected static readonly ILog Logger = LogProvider.GetLogger("Shaolinq.Query");

		public SqlDatabaseContext SqlDatabaseContext { get; }
		public ServerSqlDataDefinitionExpressionBuilder ServerSqlDataDefinitionExpressionBuilder { get; }
		
		protected SqlDatabaseSchemaManager(SqlDatabaseContext sqlDatabaseContext)
		{
			this.SqlDatabaseContext = sqlDatabaseContext;
			this.ServerSqlDataDefinitionExpressionBuilder = new ServerSqlDataDefinitionExpressionBuilder(this);
		}

		[RewriteAsync]
		public virtual void CreateDatabaseAndSchema(DatabaseCreationOptions options)
		{
			var dataDefinitionExpressions = BuildDataDefinitonExpressions(options);

			CreateDatabaseOnly(dataDefinitionExpressions, options);
			CreateDatabaseSchema(dataDefinitionExpressions, options);
		}

		public virtual Expression LoadDataDefinitionExpressions()
		{
			using (var dataTransactionContext = this.SqlDatabaseContext.CreateSqlTransactionalCommandsContext(null))
			{
				
			}

			return null;
		}

		protected virtual SqlDataDefinitionBuilderFlags GetBuilderFlags()
		{
			return SqlDataDefinitionBuilderFlags.BuildTables | SqlDataDefinitionBuilderFlags.BuildIndexes;
		}

		public virtual Expression BuildDataDefinitonExpressions(DatabaseCreationOptions options)
		{
			return SqlDataDefinitionExpressionBuilder.Build(this.SqlDatabaseContext.DataAccessModel, this.SqlDatabaseContext.SqlQueryFormatterManager, this.SqlDatabaseContext.SqlDataTypeProvider, this.SqlDatabaseContext.SqlDialect, this.SqlDatabaseContext.DataAccessModel, options, this.SqlDatabaseContext.TableNamePrefix, GetBuilderFlags());
		}
		
		[RewriteAsync]
		protected abstract bool CreateDatabaseOnly(Expression dataDefinitionExpressions, DatabaseCreationOptions options);

		[RewriteAsync]
		protected virtual void CreateDatabaseSchema(Expression dataDefinitionExpressions, DatabaseCreationOptions options)
		{
			using (var scope = new DataAccessScope())
			{
				using (var dataTransactionContext = this.SqlDatabaseContext.CreateSqlTransactionalCommandsContext(null))
				{
					using (this.SqlDatabaseContext.AcquireDisabledForeignKeyCheckContext(dataTransactionContext))
					{
						var result = this.SqlDatabaseContext.SqlQueryFormatterManager.Format(dataDefinitionExpressions, SqlQueryFormatterOptions.Default | SqlQueryFormatterOptions.EvaluateConstants);

						using (var command = dataTransactionContext.CreateCommand(SqlCreateCommandOptions.Default | SqlCreateCommandOptions.UnpreparedExecute))
						{
							command.CommandText = result.CommandText;

							Logger.Info(command.CommandText);

							command.ExecuteNonQuery();
						}
					}

					dataTransactionContext.Commit();
				}

				scope.Complete();
			}
		}

		public virtual void Dispose()
		{
		}
	}
}
