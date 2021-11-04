// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Text.RegularExpressions;
using Shaolinq.Persistence;

namespace Shaolinq.SqlServer
{
	public partial class SqlServerSqlDatabaseContext
		: SqlDatabaseContext
	{
		private static bool? isRunningMono;

		internal static bool IsRunningMono()
		{
			return isRunningMono ?? (isRunningMono = (Type.GetType("Mono.Runtime") != null)).Value;
		}

		public string Username { get; }
		public string Password { get; }
		public string ServerName { get; }
		public string Instance { get; }
		public bool DeleteDatabaseDropsTablesOnly { get; }
		public bool AllowSnapshotIsolation { get; }
		public bool ReadCommittedSnapshot { get; }

		private static readonly Regex EnlistRegex = new Regex(@"Enlist\s*=[^;$]+", RegexOptions.Compiled | RegexOptions.IgnoreCase);
		private static readonly Regex DatabaseRegex = new Regex(@"(?:(?:Initial Catalog)|(?:Database))\s*=\s*([^;$]+)[;$]", RegexOptions.IgnoreCase | RegexOptions.Compiled);

		private static string GetDatabaseName(SqlServerSqlDatabaseContextInfo contextInfo)
		{
			var connectionString = contextInfo.GetConnectionString();

			if (string.IsNullOrEmpty(connectionString))
			{
				return contextInfo.DatabaseName;
			}

			var match = DatabaseRegex.Match(connectionString);

			if (match.Success)
			{
				return match.Groups[1].Value;
			}

			return string.Empty;
		}

		public static SqlServerSqlDatabaseContext Create(SqlServerSqlDatabaseContextInfo contextInfo, DataAccessModel model)
		{
			var constraintDefaults = model.Configuration.ConstraintDefaultsConfiguration;
			var sqlDataTypeProvider = CreateSqlDataTypeProvider(model, contextInfo, () => new SqlServerSqlDataTypeProvider(constraintDefaults, contextInfo.NativeGuids));
			var sqlDialect = new SqlServerSqlDialect(contextInfo);
			var typeDescriptorProvider = model.TypeDescriptorProvider;
			var sqlQueryFormatterManager = new DefaultSqlQueryFormatterManager(sqlDialect, model.Configuration.NamingTransforms, (options, connection) => new SqlServerSqlQueryFormatter(options, sqlDialect, sqlDataTypeProvider, typeDescriptorProvider, contextInfo, ((SqlConnection)connection)?.ServerVersion));

			return new SqlServerSqlDatabaseContext(model, sqlDataTypeProvider, sqlQueryFormatterManager, contextInfo);
		}

		private SqlServerSqlDatabaseContext(DataAccessModel model, SqlDataTypeProvider sqlDataTypeProvider, SqlQueryFormatterManager sqlQueryFormatterManager, SqlServerSqlDatabaseContextInfo contextInfo)
			: base(model, new SqlServerSqlDialect(contextInfo), sqlDataTypeProvider, sqlQueryFormatterManager, GetDatabaseName(contextInfo).Trim(), contextInfo)
		{
			this.ServerName = contextInfo.ServerName;
			this.Username = contextInfo.UserName;
			this.Password = contextInfo.Password;
			this.Instance = contextInfo.Instance;
			this.AllowSnapshotIsolation = contextInfo.AllowSnapshotIsolation;
			this.ReadCommittedSnapshot = contextInfo.ReadCommittedSnapshot;
			this.DeleteDatabaseDropsTablesOnly = contextInfo.DeleteDatabaseDropsTablesOnly;

			var connectionString = contextInfo.GetConnectionString();

			if (!string.IsNullOrEmpty(connectionString))
			{
				var found = false;

				this.ConnectionString = connectionString;

				this.ConnectionString = EnlistRegex.Replace(this.ConnectionString, c =>
				{
					found = true;

					return IsRunningMono() ? "" : "Enlist=False";
				});

				if (!found && !IsRunningMono())
				{
					this.ConnectionString += ";Enlist=False;";
				}

				this.ServerConnectionString = DatabaseRegex.Replace(this.ConnectionString, "Initial Catalog=master;");
			}
			else
			{
				var connectionStringBuilder = new SqlConnectionStringBuilder();

				var dataSource = this.ServerName;

				if (!string.IsNullOrEmpty(this.Instance))
				{
					dataSource += @"\" + this.Instance;
				}

				if (string.IsNullOrEmpty(this.Username) || contextInfo.TrustedConnection)
				{
					connectionStringBuilder.IntegratedSecurity = true;
				}
				else
				{
					connectionStringBuilder.UserID = this.Username;
					connectionStringBuilder.Password = this.Password;
				}

				connectionStringBuilder.MultipleActiveResultSets = contextInfo.MultipleActiveResultSets;

				if (!IsRunningMono())
				{
					connectionStringBuilder.Enlist = false;
				}
				
				connectionStringBuilder.DataSource = dataSource;
				connectionStringBuilder.InitialCatalog = this.DatabaseName;
				connectionStringBuilder.Encrypt = contextInfo.Encrypt;
				connectionStringBuilder.Pooling = contextInfo.Pooling;

				// TODO: Remove when Mono switches to using reference source implementation

				if (!IsRunningMono())
				{
					connectionStringBuilder.TypeSystemVersion = contextInfo.TypeSystemVersion ?? "SQL Server 2008";
				}
				
				if (contextInfo.ConnectionTimeout != null)
				{
					connectionStringBuilder.ConnectTimeout = contextInfo.ConnectionTimeout.Value;
				}

				this.ConnectionString = connectionStringBuilder.ConnectionString;
				connectionStringBuilder.InitialCatalog = "master";
				this.ServerConnectionString = connectionStringBuilder.ConnectionString;
			}

			this.SchemaManager = new SqlServerSqlDatabaseSchemaManager(this);
		}

		protected override SqlTransactionalCommandsContext CreateSqlTransactionalCommandsContext(IDbConnection connection, TransactionContext transactionContext)
		{
			return new SqlServerSqlTransactionsCommandContext(this, connection, transactionContext);
		}

		public override DbProviderFactory CreateDbProviderFactory()
		{
			return SqlClientFactory.Instance;
		}

		public override IDisabledForeignKeyCheckContext AcquireDisabledForeignKeyCheckContext(SqlTransactionalCommandsContext sqlDatabaseCommandsContext)
		{
			return new DisabledForeignKeyCheckContext(sqlDatabaseCommandsContext);
		}

		public override Exception DecorateException(Exception exception, DataAccessObject dataAccessObject, string relatedQuery)
		{
			var sqlException = exception as SqlException;

			if (sqlException != null)
			{
				switch (sqlException.Number)
				{
				case 2601: // Unique index
				case 2627: // Unique constraint
					return new UniqueConstraintException(exception, relatedQuery);
				case 547:
					return new MissingRelatedDataAccessObjectException(null, dataAccessObject, exception, relatedQuery);
				case 515:
					return new MissingPropertyValueException(dataAccessObject, sqlException, relatedQuery);
				}
			}

			return base.DecorateException(exception, dataAccessObject, relatedQuery);
		}
	}
}
