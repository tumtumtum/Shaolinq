// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System.Data;
using System.Linq.Expressions;
using Shaolinq.Persistence;

namespace Shaolinq.MySql
{
	public partial class MySqlSqlTransactionalCommandsContext : DefaultSqlTransactionalCommandsContext
	{
		public MySqlSqlTransactionalCommandsContext(SqlDatabaseContext sqlDatabaseContext, IDbConnection connection, TransactionContext transactionContext)
			: base(sqlDatabaseContext, connection, transactionContext)
		{
		}

		[RewriteAsync]
		public override void Commit()
		{
			var queue = (IExpressionQueue)this.TransactionContext?.GetAttribute(MySqlSqlDatabaseContext.CommitCleanupQueueKey);

			if (queue != null)
			{
				Expression current;

				while ((current = queue.Dequeue()) != null)
				{
					var formatter = this.SqlDatabaseContext.SqlQueryFormatterManager.CreateQueryFormatter();

					using (var command = this.TransactionContext.GetSqlTransactionalCommandsContext().CreateCommand())
					{
						var formatResult = formatter.Format(current);

						command.CommandText = formatResult.CommandText;

						this.FillParameters(command, formatResult);

						command.ExecuteNonQueryEx(this.DataAccessModel, true);
					}
				}
			}

			base.Commit();
		}
	}
}