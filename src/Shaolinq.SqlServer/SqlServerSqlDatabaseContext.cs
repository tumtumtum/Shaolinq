// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Data.Common;
using System.Data.SqlClient;
using System.Text.RegularExpressions;
using System.Transactions;
using Shaolinq.Persistence;

namespace Shaolinq.SqlServer
{
	public class SqlServerSqlDatabaseContext
		: SqlDatabaseContext
	{
		public string Username { get; private set; }
		public string Password { get; private set; }
		public string ServerName { get; private set; }
		public string Instance { get; private set; }
		public bool DeleteDatabaseDropsTablesOnly { get; set; }

		public static SqlServerSqlDatabaseContext Create(SqlServerSqlDatabaseContextInfo contextInfo, DataAccessModel model)
		{
			var constraintDefaults = model.Configuration.ConstraintDefaults;
			var sqlDataTypeProvider = new SqlServerSqlDataTypeProvider(constraintDefaults);
			var sqlQueryFormatterManager = new DefaultSqlQueryFormatterManager(SqlServerSqlDialect.Default, sqlDataTypeProvider, typeof(SqlServerSqlQueryFormatter));

			return new SqlServerSqlDatabaseContext(model, sqlDataTypeProvider, sqlQueryFormatterManager, contextInfo);
		}

		private SqlServerSqlDatabaseContext(DataAccessModel model, SqlDataTypeProvider sqlDataTypeProvider, SqlQueryFormatterManager sqlQueryFormatterManager, SqlServerSqlDatabaseContextInfo contextInfo)
			: base(model, SqlServerSqlDialect.Default, sqlDataTypeProvider, sqlQueryFormatterManager, contextInfo.DatabaseName, contextInfo)
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

				this.ConnectionString = Regex.Replace(this.ConnectionString, "Enlist=[^;$]+", c =>
				{
					found = true;

					return "Enlist=False";
				});

				if (!found)
				{
					this.ConnectionString += ";Enlist=False;";
				}

				this.ServerConnectionString = Regex.Replace(this.ConnectionString, @"Initial Catalog\s*\=[^;$]+[;$]", "Initial Catalog=master;");
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

				connectionStringBuilder.Enlist = false;
				connectionStringBuilder.DataSource = dataSource;
				connectionStringBuilder.InitialCatalog = this.DatabaseName;
				connectionStringBuilder.ConnectTimeout = contextInfo.ConnectionTimeout;
				connectionStringBuilder.Encrypt = contextInfo.Encrypt;

				this.ConnectionString = connectionStringBuilder.ConnectionString;
				connectionStringBuilder.InitialCatalog = "master";
				this.ServerConnectionString = connectionStringBuilder.ConnectionString;
			}

			this.SchemaManager = new SqlServerSqlDatabaseSchemaManager(this);
		}

		public override SqlTransactionalCommandsContext CreateSqlTransactionalCommandsContext(Transaction transaction)
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
					throw new UniqueConstraintException(exception, relatedQuery);
				case 547:
					throw new MissingRelatedDataAccessObjectException(null, dataAccessObject, exception, relatedQuery);
				}
			}

			return base.DecorateException(exception, dataAccessObject, relatedQuery);
		}
	}
}
