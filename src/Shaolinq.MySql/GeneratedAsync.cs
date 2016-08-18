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
		public override Task<IDbConnection> OpenConnectionAsync()
		{
			return OpenConnectionAsync(CancellationToken.None);
		}

		public override async Task<IDbConnection> OpenConnectionAsync(CancellationToken cancellationToken)
		{
			var retval = (await base.OpenConnectionAsync(cancellationToken).ConfigureAwait(false));
			using (var command = retval.CreateCommand())
			{
				var prefix = this.SqlDialect.GetSyntaxSymbolString(SqlSyntaxSymbol.ParameterPrefix);
				var parameter = command.CreateParameter();
				parameter.DbType = DbType.String;
				parameter.ParameterName = $"{prefix}param";
				parameter.Value = this.SqlMode ?? "STRICT_ALL_TABLES";
				command.CommandText = $"SET SESSION sql_mode = {prefix}param;";
				command.Parameters.Add(parameter);
				await command.ExecuteNonQueryExAsync(this.DataAccessModel, cancellationToken, true).ConfigureAwait(false);
			}

			return retval;
		}
	}
}

namespace Shaolinq.MySql
{
#pragma warning disable
	using System;
	// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)
	using System.Data;
	using System.Threading;
	using System.Threading.Tasks;
	using System.Linq.Expressions;
	using Shaolinq;
	using Shaolinq.MySql;
	using Shaolinq.Persistence;

	public partial class MySqlSqlTransactionalCommandsContext
	{
		public override Task CommitAsync()
		{
			return CommitAsync(CancellationToken.None);
		}

		public override async Task CommitAsync(CancellationToken cancellationToken)
		{
			var queue = (IExpressionQueue)this.TransactionContext?.GetAttribute(MySqlSqlDatabaseContext.CommitCleanupQueueKey);
			if (queue != null)
			{
				Expression current;
				while ((current = queue.Dequeue()) != null)
				{
					var formatter = this.SqlDatabaseContext.SqlQueryFormatterManager.CreateQueryFormatter();
					using (var command = (await this.TransactionContext.GetSqlTransactionalCommandsContextAsync(cancellationToken).ConfigureAwait(false)).CreateCommand())
					{
						var formatResult = formatter.Format(current);
						command.CommandText = formatResult.CommandText;
						this.FillParameters(command, formatResult);
						await command.ExecuteNonQueryExAsync(this.DataAccessModel, cancellationToken, true).ConfigureAwait(false);
					}
				}
			}

			await base.CommitAsync(cancellationToken).ConfigureAwait(false);
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
		protected override Task<bool> CreateDatabaseOnlyAsync(Expression dataDefinitionExpressions, DatabaseCreationOptions options)
		{
			return CreateDatabaseOnlyAsync(dataDefinitionExpressions, options, CancellationToken.None);
		}

		protected override async Task<bool> CreateDatabaseOnlyAsync(Expression dataDefinitionExpressions, DatabaseCreationOptions options, CancellationToken cancellationToken)
		{
			var retval = false;
			var factory = this.SqlDatabaseContext.CreateDbProviderFactory();
			var overwrite = options == DatabaseCreationOptions.DeleteExistingDatabase;
			using (var dbConnection = factory.CreateConnection())
			{
				dbConnection.ConnectionString = this.SqlDatabaseContext.ServerConnectionString;
				await dbConnection.OpenAsync(cancellationToken).ConfigureAwait(false);
				using (var command = dbConnection.CreateCommand())
				{
					if (overwrite)
					{
						var drop = false;
						command.CommandText = String.Format("SHOW DATABASES;", this.SqlDatabaseContext.DatabaseName);
						using (var reader = (await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false)))
						{
							while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
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
							await command.ExecuteNonQueryExAsync(this.SqlDatabaseContext.DataAccessModel, cancellationToken, true).ConfigureAwait(false);
						}

						command.CommandText = $"CREATE DATABASE {this.SqlDatabaseContext.DatabaseName}\nDEFAULT CHARACTER SET = utf8\nDEFAULT COLLATE = utf8_general_ci;";
						await command.ExecuteNonQueryExAsync(this.SqlDatabaseContext.DataAccessModel, cancellationToken, true).ConfigureAwait(false);
						retval = true;
					}
					else
					{
						try
						{
							command.CommandText = $"CREATE DATABASE {this.SqlDatabaseContext.DatabaseName}\nDEFAULT CHARACTER SET = utf8\nDEFAULT COLLATE = utf8_general_ci;";
							await command.ExecuteNonQueryExAsync(this.SqlDatabaseContext.DataAccessModel, cancellationToken, true).ConfigureAwait(false);
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