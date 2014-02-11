using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

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
