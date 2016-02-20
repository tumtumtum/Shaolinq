// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq.Expressions;
using Shaolinq.Persistence;

namespace Shaolinq.Postgres
{
	internal class PostgresSqlDataDefinitionExpressionBuilder
	{
		public SqlDialect SqlDialect { get; }
		public SqlDatabaseContext SqlDatabaseContext { get; }
		public SqlDataTypeProvider SqlDataTypeProvider { get; }
		
		public PostgresSqlDataDefinitionExpressionBuilder(SqlDatabaseContext sqlDatabaseContext, SqlDataTypeProvider sqlDataTypeProvider, SqlDialect sqlDialect)
		{
			this.SqlDatabaseContext = sqlDatabaseContext;
			this.SqlDataTypeProvider = sqlDataTypeProvider;
			this.SqlDialect = sqlDialect;
		}

		protected virtual List<Expression> BuildCreateTableExpressions()
		{
			var factory = this.SqlDatabaseContext.CreateDbProviderFactory();
			var databaseName = this.SqlDatabaseContext.DatabaseName;

			this.SqlDatabaseContext.DropAllConnections();

			using (var dbConnection = factory.CreateConnection())
			{
				dbConnection.ConnectionString = this.SqlDatabaseContext.ServerConnectionString;
				dbConnection.Open();

				IDbCommand command;

				using (command = dbConnection.CreateCommand())
				{
					command.CommandText = String.Format("SELECT datname FROM pg_database;");

					using (var reader = command.ExecuteReader())
					{
						while (reader.Read())
						{
							var s = reader.GetString(0);

							if (s.Equals(databaseName))
							{
								break;
							}
						}
					}
				}
			}

			return null;
		}

		public static Expression Build(SqlDataTypeProvider sqlDataTypeProvider, SqlDialect sqlDialect)
		{
			return null;
		}
	}
}
