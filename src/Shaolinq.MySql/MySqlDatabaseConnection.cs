// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

 using System;
using System.Data.Common;
using System.Linq.Expressions;
using System.Transactions;
﻿using Shaolinq.Persistence;
﻿using Shaolinq.Persistence.Sql;
﻿using Shaolinq.Persistence.Sql.Linq;
using MySql.Data.MySqlClient;

﻿namespace Shaolinq.MySql
{
	public class MySqlDatabaseConnection
		: SystemDataBasedDatabaseConnection
	{
		public string ServerName { get; private set; }
		public string Username { get; private set; }
		public string Password { get; private set; }

		public override bool SupportsNestedTransactions
		{
			get
			{
				return false;
			}
		}

		protected override string GetConnectionString()
		{
			return connectionString;
		}

		public override bool SupportsDisabledForeignKeyCheckContext
		{
			get
			{
				return true;
			}
		}

		private readonly string connectionString;

		public MySqlDatabaseConnection(string serverName, string database, string username, string password, bool poolConnections, string schemaNamePrefix)
			: base(database, MySqlSqlDialect.Default, MySqlSqlDataTypeProvider.Instance)
		{
			this.ServerName = serverName;
			this.Username = username;
			this.Password = password;
			this.SchemaNamePrefix = EnvironmentSubstitutor.Substitute(schemaNamePrefix);
			
			connectionString = String.Format("Server={0}; Database={1}; Uid={2}; Pwd={3}; Pooling={4}; charset=utf8", this.ServerName, this.DatabaseName, this.Username, this.Password, poolConnections);
		}

		public override DatabaseTransactionContext NewDataTransactionContext(DataAccessModel dataAccessModel, Transaction transaction)
		{
			return new MySqlSqlDatabaseTransactionContext(this, dataAccessModel, transaction);
		}

		public override Sql92QueryFormatter NewQueryFormatter(DataAccessModel dataAccessModel, SqlDataTypeProvider sqlDataTypeProvider, SqlDialect sqlDialect, Expression expression, SqlQueryFormatterOptions options)
		{
			return new MySqlSqlQueryFormatter(dataAccessModel, sqlDataTypeProvider, sqlDialect, expression, options);
		}

		protected override DbProviderFactory NewDbProviderFactory()
		{
			return new MySqlClientFactory();
		}

		public override TableDescriptor GetTableDescriptor(string tableName)
		{
			throw new NotImplementedException();
		}

		public override SqlSchemaWriter NewSqlSchemaWriter(DataAccessModel model)
		{
			return new SqlSchemaWriter(this, model);
		}

		public override DatabaseCreator NewDatabaseCreator(DataAccessModel model)
		{
			return new MySqlSqlDatabaseCreator(this, model);
		}

		public override MigrationPlanApplicator NewMigrationPlanApplicator(DataAccessModel model)
		{
			throw new NotImplementedException();
		}

		public override MigrationPlanCreator NewMigrationPlanCreator(DataAccessModel model)
		{
			return new SqlDatabaseMigrationPlanCreator(this, model);
		}

		public override bool CreateDatabase(bool overwrite)
		{
			var retval = false;
			var factory = this.NewDbProviderFactory();

			using (var connection = factory.CreateConnection())
			{
				connection.ConnectionString = String.Concat("Server=", this.ServerName, ";Database=mysql;Uid=", this.Username, ";Pwd=", this.Password);

				connection.Open();
                
				var command = connection.CreateCommand();

				if (overwrite)
				{
					var drop = false;

					command.CommandText = String.Format("SHOW DATABASES;", this.DatabaseName);

					using (var reader = command.ExecuteReader())
					{
						while (reader.Read())
						{
							var s = reader.GetString(0);

							if (s.Equals(this.DatabaseName) ||
                                s.Equals(this.DatabaseName.ToLower()))
							{
								drop = true;

								break;
							}
						}
					}

					if (drop)
					{
						command.CommandText = String.Concat("DROP DATABASE ", this.DatabaseName);
						command.ExecuteNonQuery();
					}

					command.CommandText = String.Concat("CREATE DATABASE ", this.DatabaseName, "\nDEFAULT CHARACTER SET = utf8\nDEFAULT COLLATE = utf8_general_ci;");
					command.ExecuteNonQuery();

					retval = true;
				}
				else
				{
					try
					{
						command.CommandText = String.Concat("CREATE DATABASE ", this.DatabaseName, "\nDEFAULT CHARACTER SET = utf8\nDEFAULT COLLATE = utf8_general_ci;");
						command.ExecuteNonQuery();

						retval = true;
					}
					catch
					{
						retval = false;
					}
				}
			}

			return retval;
		}
        
		public override IDisabledForeignKeyCheckContext AcquireDisabledForeignKeyCheckContext(DatabaseTransactionContext databaseTransactionContext)
		{
			return new DisabledForeignKeyCheckContext(databaseTransactionContext);	
		}

		public override IPersistenceQueryProvider NewQueryProvider(DataAccessModel dataAccessModel, DatabaseConnection databaseConnection)
		{
			return new SqlQueryProvider(dataAccessModel, databaseConnection);
		}

		public override void DropAllConnections()
		{
		}
	}
}
