// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

 using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Transactions;
﻿using Shaolinq.Persistence;
﻿using Shaolinq.Persistence.Sql;
﻿using Shaolinq.Persistence.Sql.Linq;
using Devart.Data.PostgreSql;
using Shaolinq.Postgres.Shared;

namespace Shaolinq.Postgres.DotConnect
{
    public class PostgresDotConnectDatabaseConnection
        : SystemDataBasedDatabaseConnection
    {
	    public string Host { get; set; }
	    public string Userid { get; set; }
	    public string Password { get; set; }
	    public string Database { get; set; }
	    public int Port { get; set; }

		public override string GetConnectionString()
        {
            return connectionString;
        }

        internal readonly string connectionString;
        internal readonly string databaselessConnectionString;

		public PostgresDotConnectDatabaseConnection(string host, string userid, string password, string database, int port, bool pooling, int minPoolSize, int maxPoolSize, int connectionTimeoutSeconds, int commandTimeoutSeconds, bool nativeUuids, string schemaNamePrefix, DateTimeKind dateTimeKindForUnspecifiedDateTimeKinds)
			: base(database, PostgresSharedSqlDialect.Default, new PostgresSharedSqlDataTypeProvider(nativeUuids, dateTimeKindForUnspecifiedDateTimeKinds))
        {
            this.Host = host;
            this.Userid = userid;
            this.Password = password;
            this.Database = database;
            this.Port = port; 
            this.CommandTimeout = TimeSpan.FromSeconds(commandTimeoutSeconds);
			this.SchemaNamePrefix = EnvironmentSubstitutor.Substitute(schemaNamePrefix);

            var sb = new PgSqlConnectionStringBuilder
                         {
                             Host = host,
                             UserId = userid,
                             Password = password,
                             Port = port,
                             Pooling = pooling,
                             Enlist = false,
                             ConnectionTimeout = connectionTimeoutSeconds,
                             Charset = "UTF8",
                             Unicode = true,
                             MaxPoolSize = maxPoolSize,
                             DefaultCommandTimeout = commandTimeoutSeconds
                         };
            databaselessConnectionString = sb.ConnectionString;
            
            sb.Database = database;
            connectionString = sb.ConnectionString;
        }

        public override DatabaseTransactionContext NewDataTransactionContext(DataAccessModel dataAccessModel, Transaction transaction)
        {
            return new PostgresDotConnectSqlDatabaseTransactionContext(this, dataAccessModel, transaction);
        }

		public override Sql92QueryFormatter NewQueryFormatter(DataAccessModel dataAccessModel, SqlDataTypeProvider sqlDataTypeProvider, SqlDialect sqlDialect, Expression expression, SqlQueryFormatterOptions options)
        {
            return new PostgresSharedSqlQueryFormatter(dataAccessModel, sqlDataTypeProvider, sqlDialect, expression, options);
        }

		public override DbProviderFactory NewDbProviderFactory()
        {
            return PgSqlProviderFactory.Instance;
        }

        public override TableDescriptor GetTableDescriptor(string tableName)
        {
			var retval = new TableDescriptor();

			var factory = this.NewDbProviderFactory();

			using (var connection = factory.CreateConnection())
			{
				connection.ConnectionString = connectionString;

				connection.Open();

				// Get Columns

				using (var command = connection.CreateCommand())
				{
					var sql = string.Format(@"SELECT * FROM ""{0}"" LIMIT 1", tableName);

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
								var columnDescriptor = new ColumnDescriptor
								{
									ColumnName = (string) row.ItemArray[indexByName["ColumnName"]],
									DataLength = (int) row.ItemArray[indexByName["ColumnSize"]],
									DataType = (Type) row.ItemArray[indexByName["DataType"]],
								};

								retval.Columns.Add(columnDescriptor);
							}
						}
					}
					catch (PgSqlException)
					{
						return null;
					}
				}

				// Get Column attnum

				var columnDescriptorsByAttnum = new Dictionary<int, ColumnDescriptor>();

				using (var command = connection.CreateCommand())
				{
					const int attnumOrdinal = 0;
					const int columnNameOrdinal = 1;

					var sql = String.Format(@"
						SELECT a.attnum, a.attname
						FROM pg_class c join pg_attribute a on c.oid = a.attrelid
						WHERE c.relname = @tableName
						AND a.attnum >= 0
						AND a.attisdropped = false");

					var param = command.CreateParameter();

					param.ParameterName = "@tableName";
					param.Value = tableName;

					command.CommandText = sql;
					command.Parameters.Add(param);

					try
					{
						using (var reader = command.ExecuteReader())
						{
							while (reader.Read())
							{
								var attnum = reader.GetInt16(attnumOrdinal);
								var columnName = reader.GetString(columnNameOrdinal);

								var columnDescriptor = retval.Columns.FirstOrDefault(x => x.ColumnName == columnName);

								if (columnDescriptor != null)
								{
									columnDescriptorsByAttnum[attnum] = columnDescriptor;
								}
							}
						}
					}
					catch (PgSqlException)
					{
					}
				}

				// Get Indexes

				using (var command = connection.CreateCommand())
				{
					const int nameOrdinal = 0;
					const int columnsOrdinal = 1;

					var sql = String.Format(@"
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
						ORDER BY 1,2;");

					var param = command.CreateParameter();

					param.ParameterName = "@tableName";
					param.Value = tableName;

					command.CommandText = sql;
					command.Parameters.Add(param);

					try
					{
						using (var reader = command.ExecuteReader())
						{
							while (reader.Read())
							{
								var indexName = Convert.ToString(reader.GetValue(nameOrdinal));
								var columnIds = (short[])(reader.GetValue(columnsOrdinal));

								var indexDescriptor = new TableIndexDescriptor();

								indexDescriptor.Name = indexName;

								foreach (var columnId in columnIds)
								{
									if (columnId == 0)
									{
										break;
									}

									var columnDescriptor = columnDescriptorsByAttnum[columnId];

									indexDescriptor.Columns.Add(columnDescriptor);
								}

								retval.Indexes.Add(indexDescriptor);
							}
						}
					}
					catch (PgSqlException)
					{	
					}
				}
			}

			return retval;
        }

        public override SqlSchemaWriter NewSqlSchemaWriter(DataAccessModel model)
        {
            return new SqlSchemaWriter(this, model);
        }

	    public override DatabaseCreator NewDatabaseCreator(DataAccessModel model)
	    {
		    return new PostgresDotConnectDatabaseCreator(this, model);
	    }

	    public override MigrationPlanApplicator NewMigrationPlanApplicator(DataAccessModel model)
        {
            return new SqlMigrationPlanApplicator(this, model);
        }

        public override MigrationPlanCreator NewMigrationPlanCreator(DataAccessModel model)
        {
            return new SqlDatabaseMigrationPlanCreator(this, model);
        }

        public override bool CreateDatabase(bool overwrite)
        {
            bool retval = false;
            DbProviderFactory factory;

            factory = this.NewDbProviderFactory();

        	this.DropAllConnections();

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

                                if (s.Equals(this.DatabaseName))
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
                            command.CommandText = String.Concat("DROP DATABASE \"", this.DatabaseName, "\";");
                            command.ExecuteNonQuery();
                        }
                    }

                    using (command = connection.CreateCommand())
                    {
                        command.CommandText = String.Concat("CREATE DATABASE \"", this.DatabaseName, "\" WITH ENCODING 'UTF8';");
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
                            command.CommandText = String.Concat("CREATE DATABASE \"", this.DatabaseName, "\" WITH ENCODING 'UTF8';");
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

        public override IDisabledForeignKeyCheckContext AcquireDisabledForeignKeyCheckContext(DatabaseTransactionContext databaseTransactionContext)
        {
            return new DisabledForeignKeyCheckContext(databaseTransactionContext);	
        }

		public override void DropAllConnections()
		{
			PgSqlConnection.ClearAllPools();
		}
    }
}
