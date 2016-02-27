// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Data.Common;
using System.Data.SqlClient;
using System.Text.RegularExpressions;
using Shaolinq.Persistence;

namespace Shaolinq.SqlServer
{
	public class SqlServerSqlDatabaseContext
		: SqlDatabaseContext
	{
		public string Username { get; }
		public string Password { get; }
		public string ServerName { get; }
		public string Instance { get; }
		public bool DeleteDatabaseDropsTablesOnly { get; set; }

		private static readonly Regex EnlistRegex = new Regex(@"Enlist\s*=[^;$]+", RegexOptions.Compiled | RegexOptions.IgnoreCase);
		private static readonly Regex DatabaseRegex= new Regex(@".*((Initial Catalog)|(Database))\s*\=([^;$]+)[;$]", RegexOptions.Compiled | RegexOptions.IgnoreCase);

		private static string GetDatabaseName(SqlServerSqlDatabaseContextInfo contextInfo)
		{
			if (string.IsNullOrEmpty(contextInfo.ConnectionString))
			{
				return contextInfo.DatabaseName;
			}

			var match = DatabaseRegex.Match(contextInfo.ConnectionString);

			if (match.Success)
			{
				return match.Groups[1].Value;
			}

			return string.Empty;
		}

		public static SqlServerSqlDatabaseContext Create(SqlServerSqlDatabaseContextInfo contextInfo, DataAccessModel model)
		{
			var constraintDefaults = model.Configuration.ConstraintDefaultsConfiguration;
			var sqlDataTypeProvider = new SqlServerSqlDataTypeProvider(constraintDefaults);
			var sqlQueryFormatterManager = new DefaultSqlQueryFormatterManager(new SqlServerSqlDialect(contextInfo), sqlDataTypeProvider, typeof(SqlServerSqlQueryFormatter));

			return new SqlServerSqlDatabaseContext(model, sqlDataTypeProvider, sqlQueryFormatterManager, contextInfo);
		}

		private SqlServerSqlDatabaseContext(DataAccessModel model, SqlDataTypeProvider sqlDataTypeProvider, SqlQueryFormatterManager sqlQueryFormatterManager, SqlServerSqlDatabaseContextInfo contextInfo)
			: base(model, new SqlServerSqlDialect(contextInfo), sqlDataTypeProvider, sqlQueryFormatterManager, GetDatabaseName(contextInfo).Trim(), contextInfo)
		{
			this.ServerName = contextInfo.ServerName;
			this.Username = contextInfo.UserName;
			this.Password = contextInfo.Password;
			this.Instance = contextInfo.Instance;
			this.DeleteDatabaseDropsTablesOnly = contextInfo.DeleteDatabaseDropsTablesOnly;

			if (!string.IsNullOrEmpty(contextInfo.ConnectionString))
			{
				var found = false;

				this.ConnectionString = contextInfo.ConnectionString;

				this.ConnectionString = EnlistRegex.Replace(this.ConnectionString, c =>
				{
					found = true;

					return "Enlist=False";
				});

				if (!found)
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
				connectionStringBuilder.Enlist = false;
				connectionStringBuilder.DataSource = dataSource;
				connectionStringBuilder.InitialCatalog = this.DatabaseName;
				connectionStringBuilder.Encrypt = contextInfo.Encrypt;

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

		public override SqlTransactionalCommandsContext CreateSqlTransactionalCommandsContext(DataAccessTransaction transaction)
		{
			return new SqlServerSqlTransactionsCommandContext(this, transaction);
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
				case 2627:
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
