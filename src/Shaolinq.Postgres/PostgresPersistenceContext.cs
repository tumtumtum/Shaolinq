// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)


 using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Transactions;
using Npgsql;
﻿using Shaolinq.Persistence;
﻿using Shaolinq.Persistence.Sql;
 using Shaolinq.Persistence.Sql.Linq;
﻿using Shaolinq.Postgres.Shared;

﻿namespace Shaolinq.Postgres
{
	public class PostgresPersistenceContext
		: SqlPersistenceContext
	{
		public string Host { get; set; }
		public string Userid { get; set; }
		public string Password { get; set; }
		public string Database { get; set; }
		public int Port { get; set; }

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
		private readonly string databaselessConnectionString;

		public PostgresPersistenceContext(string host, string userid, string password, string database, int port, bool pooling, int minPoolSize, int maxPoolSize, int connectionTimeoutSeconds, bool nativeUuids, int commandTimeoutSeconds, string schemaNamePrefix, DateTimeKind dateTimeKindIfUnspecifed)
			: base(database, PostgresSqlDialect.Default, new PostgresSqlDataTypeProvider(nativeUuids, dateTimeKindIfUnspecifed))
		{
			this.Host = host;
			this.Userid = userid;
			this.Password = password;
			this.Database = database;
			this.Port = port;
			this.SchemaNamePrefix = EnvironmentSubstitutor.Substitute(schemaNamePrefix);
			this.CommandTimeout = TimeSpan.FromSeconds(commandTimeoutSeconds);

			connectionString = String.Format("Host={0};User Id={1};Password={2};Database={3};Port={4};Pooling={5};MinPoolSize={6};MaxPoolSize={7};Enlist=false;Timeout={8};CommandTimeout={9}", host, userid, password, database, port, pooling, minPoolSize, maxPoolSize, connectionTimeoutSeconds, commandTimeoutSeconds);
			databaselessConnectionString = String.Format("Host={0};User Id={1};Password={2};Port={4};Pooling={5};MinPoolSize={6};MaxPoolSize={7};Enlist=false;Timeout={8};CommandTimeout={9}", host, userid, password, database, port, pooling, minPoolSize, maxPoolSize, connectionTimeoutSeconds, commandTimeoutSeconds);
		}

		public override PersistenceTransactionContext NewDataTransactionContext(DataAccessModel dataAccessModel, Transaction transaction)
		{
			return new PostgresSqlPersistenceTransactionContext(this, dataAccessModel, transaction);
		}

		public override Sql92QueryFormatter NewQueryFormatter(DataAccessModel dataAccessModel, SqlDataTypeProvider sqlDataTypeProvider, SqlDialect sqlDialect, Expression expression, SqlQueryFormatterOptions options)
		{
			return new PostgresSqlQueryFormatter(dataAccessModel, sqlDataTypeProvider, sqlDialect, expression, options);
		}

		protected override DbProviderFactory NewDbProproviderFactory()
		{
			return NpgsqlFactory.Instance;
		}

		public override TableDescriptor GetTableDescriptor(string tableName)
		{
			var retval = new TableDescriptor();
			var sql = string.Format(@"SELECT * FROM ""{0}"" LIMIT 1", tableName);

			var factory = NewDbProproviderFactory();

			using (var connection = factory.CreateConnection())
			{
				connection.ConnectionString = connectionString;

				connection.Open();

				using (var command = connection.CreateCommand()) 
				{
					command.CommandText = sql;

					try
					{
						using (var reader = command.ExecuteReader())
						{
							var dataTable = reader.GetSchemaTable();
							var indexByName = new Dictionary<string, int>();

							var x = 0;

							foreach (DataColumn column in dataTable.Columns)
							{
								indexByName[column.ColumnName] = x++;
							}

							foreach (DataRow row in dataTable.Rows)
							{
								var columnDescriptor = new ColumnDescriptor()
								{
									DataLength = (int)row.ItemArray[indexByName["ColumnSize"]],
									DataType = (Type)row.ItemArray[indexByName["DataType"]],
									ColumnName = (string)row.ItemArray[indexByName["ColumnName"]]
								};

								retval.Columns.Add(columnDescriptor);
							}
						}
					}
					catch (Exception)
					{
						return null;
					}
				}

				// Get Indexes

				const int nameOrdinal = 0;
				const int columnsOrdinal = 1;
					
				sql = 
				String.Format(@"
					SELECT 
					 c.relname AS ""Name"",
					i.indkey AS ""Columns""
					FROM pg_catalog.pg_class c
						JOIN pg_catalog.pg_index i ON i.indexrelid = c.oid
						JOIN pg_catalog.pg_class c2 ON i.indrelid = c2.oid
						LEFT JOIN pg_catalog.pg_user u ON u.usesysid = c.relowner
						LEFT JOIN pg_catalog.pg_namespace n ON n.oid = c.relnamespace
					WHERE c.relkind IN ('i','')
						 AND n.nspname NOT IN ('pg_catalog', 'pg_toast')
						 AND pg_catalog.pg_table_is_visible(c.oid)
						 AND c.relkind = 'i'
						AND c2.relname = @tableName
					ORDER BY 1,2;
				");

				using (var command = connection.CreateCommand())
				{
					var param = command.CreateParameter();

					param.ParameterName = "tableName";
					param.Value = tableName;

					command.CommandText = sql;
					
					command.Parameters.Add(param);

					using (var reader = command.ExecuteReader())
					{
						while (reader.Read())
						{
							var indexName = Convert.ToString(reader.GetValue(nameOrdinal));
							var columns = Convert.ToString(reader.GetValue(columnsOrdinal));

							var indexDescriptor = new TableIndexDescriptor();

							indexDescriptor.Name = indexName;
                            
							foreach (var columnId in columns.Split(' ').Select(c => Convert.ToInt32(c)))
							{
								if (columnId == 0)
								{
									break;
								}
								else
								{
									var columnDescriptor = retval.Columns[columnId - 1];

									indexDescriptor.Columns.Add(columnDescriptor);
								}
							}

							if (indexDescriptor != null)
							{
								retval.Indexes.Add(indexDescriptor);
							}
						}
					}
				}
			}

			return retval;
		}

		public override SqlSchemaWriter NewSqlSchemaWriter(DataAccessModel model, DataAccessModelPersistenceContextInfo persistenceContextInfo)
		{
			return new SqlSchemaWriter(this, model, persistenceContextInfo);
		}

		public override PersistenceStoreCreator NewPersistenceStoreCreator(DataAccessModel model, DataAccessModelPersistenceContextInfo persistenceContextInfo)
		{
			return new PostgresSqlDatabaseCreator(this, model, persistenceContextInfo);
		}

		public override MigrationPlanApplicator NewMigrationPlanApplicator(DataAccessModel model, DataAccessModelPersistenceContextInfo dataAccessModelPersistenceContextInfo)
		{
			return new SqlMigrationPlanApplicator(this, model, dataAccessModelPersistenceContextInfo);
		}

		public override MigrationPlanCreator NewMigrationPlanCreator(DataAccessModel model, DataAccessModelPersistenceContextInfo dataAccessModelPersistenceContextInfo)
		{
			return new SqlPersistenceContextMigrationPlanCreator(this, model, dataAccessModelPersistenceContextInfo);
		}

		public override bool CreateDatabase(bool overwrite)
		{
			bool retval = false;
			DbProviderFactory factory;

			factory = NewDbProproviderFactory();
            
			using (var connection = factory.CreateConnection())
			{
				connection.ConnectionString = databaselessConnectionString;

				connection.Open();

				IDbCommand command;

				if (overwrite)
				{
					bool drop = false;

					using (command = connection.CreateCommand())
					{
						command.CommandText = String.Format("SELECT datname FROM pg_database;");

						using (var reader = command.ExecuteReader())
						{
							while (reader.Read())
							{
								string s = reader.GetString(0);

								if (s.Equals(this.PersistenceStoreName))
								{
									drop = true;

									break;
								}
							}
						}
					}

					if (drop)
					{
						using (command = connection.CreateCommand())
						{
							NpgsqlConnection.ClearAllPools();
							command.CommandText = String.Concat("DROP DATABASE \"", this.PersistenceStoreName, "\";");
							command.ExecuteNonQuery();
						}
					}

					using (command = connection.CreateCommand())
					{
						command.CommandText = String.Concat("CREATE DATABASE \"", this.PersistenceStoreName, "\" WITH ENCODING 'UTF8';");
						command.ExecuteNonQuery();
					}

					retval = true;
				}
				else
				{
					try
					{
						using (command = connection.CreateCommand())
						{
							command.CommandText = String.Concat("CREATE DATABASE \"", this.PersistenceStoreName, "\" WITH ENCODING 'UTF8';");
							command.ExecuteNonQuery();
						}

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

		public override IDisabledForeignKeyCheckContext AcquireDisabledForeignKeyCheckContext(PersistenceTransactionContext persistenceTransactionContext)
		{
			return new DisabledForeignKeyCheckContext(persistenceTransactionContext);	
		}

		public override IPersistenceQueryProvider NewQueryProvider(DataAccessModel dataAccessModel, PersistenceContext persistenceContext)
		{
			return new SqlQueryProvider(dataAccessModel, persistenceContext);
		}

		public override void DropAllConnections()
		{
			NpgsqlConnection.ClearAllPools();
		}
	}
}
