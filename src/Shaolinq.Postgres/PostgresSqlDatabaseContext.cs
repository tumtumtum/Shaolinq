// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text.RegularExpressions;
using Npgsql;
using Shaolinq.Persistence;

namespace Shaolinq.Postgres
{
	public class PostgresSqlDatabaseContext
		: SqlDatabaseContext
	{
		public int Port { get; }
		public string Host { get; }
		public string UserId { get; }
		public string Password { get; }
		
		public static PostgresSqlDatabaseContext Create(PostgresSqlDatabaseContextInfo contextInfo, DataAccessModel model)
		{
			var constraintDefaults = model.Configuration.ConstraintDefaultsConfiguration;
			var sqlDialect = new PostgresSqlDialect();
			var sqlDataTypeProvider = new PostgresSqlDataTypeProvider(model.TypeDescriptorProvider, constraintDefaults, contextInfo.NativeUuids, contextInfo.NativeEnums);
			var typeDescriptorProvider = model.TypeDescriptorProvider;
			var sqlQueryFormatterManager = new DefaultSqlQueryFormatterManager(sqlDialect, (options) => new PostgresSqlQueryFormatter(options, sqlDialect, sqlDataTypeProvider, typeDescriptorProvider, contextInfo.SchemaName, true));

			return new PostgresSqlDatabaseContext(model, sqlDialect, sqlDataTypeProvider, sqlQueryFormatterManager, contextInfo);
		}

		protected PostgresSqlDatabaseContext(DataAccessModel model, SqlDialect sqlDialect, SqlDataTypeProvider sqlDataTypeProvider, SqlQueryFormatterManager sqlQueryFormatterManager, PostgresSqlDatabaseContextInfo contextInfo)
			: base(model, sqlDialect, sqlDataTypeProvider, sqlQueryFormatterManager, contextInfo.DatabaseName, contextInfo)
		{
			this.SupportsPreparedTransactions = contextInfo.EnablePreparedTransactions;

			this.Host = contextInfo.ServerName;
			this.UserId = contextInfo.UserId;
			this.Password = contextInfo.Password;
			this.Port = contextInfo.Port;
			
			var connectionStringBuilder = new NpgsqlConnectionStringBuilder
			{
				Host = contextInfo.ServerName,
				Username = contextInfo.UserId,
				Password = contextInfo.Password,
				Port = contextInfo.Port,
				Pooling = contextInfo.Pooling,
				Enlist = false,
				MinPoolSize = contextInfo.MinPoolSize,
				MaxPoolSize = contextInfo.MaxPoolSize,
				KeepAlive = contextInfo.KeepAlive,
				ConnectionIdleLifetime = contextInfo.ConnectionIdleLifetime,
				ConvertInfinityDateTime = contextInfo.ConvertInfinityDateTime
			};

			if (contextInfo.Timeout != null)
			{
				connectionStringBuilder.Timeout = contextInfo.Timeout.Value;
			}

			if (contextInfo.ConnectionTimeout.HasValue)
			{
				connectionStringBuilder.Timeout = contextInfo.ConnectionTimeout.Value;
			}
			
			if (contextInfo.ConnectionCommandTimeout.HasValue)
			{
				connectionStringBuilder.CommandTimeout = contextInfo.ConnectionCommandTimeout.Value;
			}

			connectionStringBuilder.Database = contextInfo.DatabaseName;

			this.ConnectionString = connectionStringBuilder.ToString();

			connectionStringBuilder.Database = "postgres";

			this.ServerConnectionString = connectionStringBuilder.ToString();

			this.SchemaManager = new PostgresSqlDatabaseSchemaManager(this);
		}

		protected override SqlTransactionalCommandsContext CreateSqlTransactionalCommandsContext(IDbConnection connection, TransactionContext transactionContext)
		{
			return new PostgresSqlTransactionalCommandsContext(this, connection, transactionContext);
		}

		public override DbProviderFactory CreateDbProviderFactory()
		{
			return NpgsqlFactory.Instance;
		}

		public override IDisabledForeignKeyCheckContext AcquireDisabledForeignKeyCheckContext(SqlTransactionalCommandsContext sqlDatabaseCommandsContext)
		{
			return new DisabledForeignKeyCheckContext(sqlDatabaseCommandsContext);	
		}

		public override void DropAllConnections()
		{
			NpgsqlConnection.ClearAllPools();
		}
		
		public override Exception DecorateException(Exception exception, DataAccessObject dataAccessObject, string relatedQuery)
		{
			var postgresException = exception as PostgresException;

			if (postgresException == null)
			{
				return base.DecorateException(exception, dataAccessObject, relatedQuery);
			}

			switch (postgresException.SqlState)
			{
			case "40001":
				return new ConcurrencyException(exception, relatedQuery);
			case "23502":
				return new MissingPropertyValueException(dataAccessObject, postgresException, relatedQuery);
			case "23503":
				return new MissingRelatedDataAccessObjectException(null, dataAccessObject, postgresException, relatedQuery);
			case "23505":
				if (!string.IsNullOrEmpty(postgresException.ColumnName) && dataAccessObject != null)
				{
					if (dataAccessObject.GetAdvanced().GetPrimaryKeysFlattened().Any(c => c.PersistedName == postgresException.ColumnName))
					{
						return new ObjectAlreadyExistsException(dataAccessObject, exception, relatedQuery);
					}
				}

				if (!string.IsNullOrEmpty(postgresException.ConstraintName) && postgresException.ConstraintName.EndsWith("_pkey"))
				{
					return new ObjectAlreadyExistsException(dataAccessObject, exception, relatedQuery);
				}

				if (!string.IsNullOrEmpty(postgresException.Detail) && dataAccessObject != null)
				{
					if (dataAccessObject.GetAdvanced().GetPrimaryKeysFlattened().Any(c => Regex.Match(postgresException.Detail, @"Key\s*\(\s*""?" + c.PersistedName + @"""?\s*\)", RegexOptions.CultureInvariant).Success))
					{
						return new ObjectAlreadyExistsException(dataAccessObject, exception, relatedQuery);	
					}
				}

				if (postgresException.Message.IndexOf("_pkey", StringComparison.InvariantCultureIgnoreCase) >= 0)
				{
					return new ObjectAlreadyExistsException(dataAccessObject, exception, relatedQuery);	
				}
				else
				{
					return new UniqueConstraintException(exception, relatedQuery);
				}
			}
			
			return new DataAccessException(postgresException, relatedQuery);
		}
	}
}
