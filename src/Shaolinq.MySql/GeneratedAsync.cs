namespace Shaolinq.MySql
{
#pragma warning disable
	using System;
	using System.Data;
	using System.Threading;
	using System.Data.Common;
	using System.Threading.Tasks;
	using System.Text.RegularExpressions;
	using Shaolinq;
	using Shaolinq.MySql;
	using Shaolinq.Persistence;
	using global::MySql.Data.MySqlClient;

	public partial class MySqlSqlDatabaseContext
	{
		public virtual Task<IDbConnection> OpenConnectionAsync()
		{
			return this.OpenConnectionAsync(CancellationToken.None);
		}

		public async virtual Task<IDbConnection> OpenConnectionAsync(CancellationToken cancellationToken)
		{
			var retval = base.OpenConnection();
			using (var command = retval.CreateCommand())
			{
				var prefix = this.SqlDialect.GetSyntaxSymbolString(SqlSyntaxSymbol.ParameterPrefix);
				var parameter = command.CreateParameter();
				parameter.DbType = DbType.String;
				parameter.ParameterName = $"{prefix}param";
				parameter.Value = this.SqlMode ?? "STRICT_ALL_TABLES";
				command.CommandText = $"SET SESSION sql_mode = {prefix}param;";
				command.Parameters.Add(parameter);
				command.ExecuteNonQueryEx(this.DataAccessModel, true);
			}

			return retval;
		}
	}
}

namespace Shaolinq.MySql
{
#pragma warning disable
	using System;
	// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)
	using System.Data;
	using System.Threading;
	using System.Threading.Tasks;
	using System.Linq.Expressions;
	using Shaolinq;
	using Shaolinq.MySql;
	using Shaolinq.Persistence;

	public partial class MySqlSqlTransactionalCommandsContext
	{
		public virtual Task CommitAsync()
		{
			return this.CommitAsync(CancellationToken.None);
		}

		public async virtual Task CommitAsync(CancellationToken cancellationToken)
		{
			var queue = (IExpressionQueue)this.TransactionContext?.GetAttribute(MySqlSqlDatabaseContext.CommitCleanupQueueKey);
			if (queue != null)
			{
				Expression current;
				while ((current = queue.Dequeue()) != null)
				{
					var formatter = this.SqlDatabaseContext.SqlQueryFormatterManager.CreateQueryFormatter();
					using (var command = this.TransactionContext.GetSqlTransactionalCommandsContext().CreateCommand())
					{
						var formatResult = formatter.Format(current);
						command.CommandText = formatResult.CommandText;
						this.FillParameters(command, formatResult);
						command.ExecuteNonQueryEx(this.DataAccessModel, true);
					}
				}
			}

			base.Commit();
		}
	}
}

namespace Shaolinq.MySql
{
#pragma warning disable
	using System;
	using System.Threading;
	using System.Threading.Tasks;
	using System.Linq.Expressions;
	using Shaolinq;
	using Shaolinq.MySql;
	using Shaolinq.Persistence;

	public partial class MySqlSqlDatabaseSchemaManager
	{
		protected virtual Task<bool> CreateDatabaseOnlyAsync(Expression dataDefinitionExpressions, DatabaseCreationOptions options)
		{
			return this.CreateDatabaseOnlyAsync(dataDefinitionExpressions, options, CancellationToken.None);
		}

		protected async virtual Task<bool> CreateDatabaseOnlyAsync(Expression dataDefinitionExpressions, DatabaseCreationOptions options, CancellationToken cancellationToken)
		{
			var retval = false;
			var factory = this.SqlDatabaseContext.CreateDbProviderFactory();
			var overwrite = options == DatabaseCreationOptions.DeleteExistingDatabase;
			using (var dbConnection = factory.CreateConnection())
			{
				dbConnection.ConnectionString = this.SqlDatabaseContext.ServerConnectionString;
				dbConnection.Open();
				using (var command = dbConnection.CreateCommand())
				{
					if (overwrite)
					{
						var drop = false;
						command.CommandText = String.Format("SHOW DATABASES;", this.SqlDatabaseContext.DatabaseName);
						using (var reader = command.ExecuteReader())
						{
							while (reader.Read())
							{
								var s = reader.GetString(0);
								if (s.Equals(this.SqlDatabaseContext.DatabaseName) || s.Equals(this.SqlDatabaseContext.DatabaseName.ToLower()))
								{
									drop = true;
									break;
								}
							}
						}

						if (drop)
						{
							command.CommandText = $"DROP DATABASE {this.SqlDatabaseContext.DatabaseName}";
							command.ExecuteNonQueryEx(this.SqlDatabaseContext.DataAccessModel, true);
						}

						command.CommandText = $"CREATE DATABASE {this.SqlDatabaseContext.DatabaseName}\nDEFAULT CHARACTER SET = utf8\nDEFAULT COLLATE = utf8_general_ci;";
						command.ExecuteNonQueryEx(this.SqlDatabaseContext.DataAccessModel, true);
						retval = true;
					}
					else
					{
						try
						{
							command.CommandText = $"CREATE DATABASE {this.SqlDatabaseContext.DatabaseName}\nDEFAULT CHARACTER SET = utf8\nDEFAULT COLLATE = utf8_general_ci;";
							command.ExecuteNonQueryEx(this.SqlDatabaseContext.DataAccessModel, true);
							retval = true;
						}
						catch
						{
							retval = false;
						}
					}
				}
			}

			return retval;
		}
	}
}