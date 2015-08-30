// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Data.Common;

namespace Shaolinq.Persistence
{
	public class ServerSqlDataDefinitionExpressionBuilder
	{
		public SqlDatabaseSchemaManager SchemaManager { get; private set; }

		public ServerSqlDataDefinitionExpressionBuilder(SqlDatabaseSchemaManager schemaManager)
		{
			this.SchemaManager = schemaManager;
		}

		public virtual void Build()
		{
			var sqlDatabaseContext = this.SchemaManager.SqlDatabaseContext;
			
			using (var connection =  (DbConnection)sqlDatabaseContext.OpenConnection())
			{
				var schema = connection.GetSchema("Tables");

				Console.WriteLine(schema);
			}
		}
	}
}
