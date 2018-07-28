// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Data.Common;

namespace Shaolinq.Persistence
{
	public class ServerSqlDataDefinitionExpressionBuilder
	{
		public SqlDatabaseSchemaManager SchemaManager { get; }

		public ServerSqlDataDefinitionExpressionBuilder(SqlDatabaseSchemaManager schemaManager)
		{
			this.SchemaManager = schemaManager;
		}

		public virtual void Build()
		{
			var sqlDatabaseContext = this.SchemaManager.SqlDatabaseContext;
			
			using (var connection =  sqlDatabaseContext.OpenConnection())
			{
				var dbConnection = (connection as DbConnection) ?? (connection as DbConnectionWrapper)?.Inner as DbConnection;

				var schema = dbConnection.GetSchema("Columns");

				Console.WriteLine(schema);
			}
		}
	}
}
