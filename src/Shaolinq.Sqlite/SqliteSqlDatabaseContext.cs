// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Data;
using System.IO;
using System.Text.RegularExpressions;
using System.Transactions;
using Shaolinq.Persistence;

namespace Shaolinq.Sqlite
{
	public abstract class SqliteSqlDatabaseContext
		: SqlDatabaseContext
	{
		public string FileName { get; protected set; }
		public bool IsInMemoryConnection { get; protected set; }
		public bool IsSharedCacheConnection { get; protected set; }
		
		private IDbConnection connection;

		public override IDbConnection OpenConnection()
		{
			var retval = this.PrivateOpenConnection();

			if (retval == null)
			{
				return null;
			}

			using (var command = retval.CreateCommand())
			{
				command.CommandText = "PRAGMA foreign_keys = ON;";
				command.ExecuteNonQuery();
			}

			return retval;
		}

		private IDbConnection PrivateOpenConnection()
		{
			if (!this.IsInMemoryConnection)
			{
				return base.OpenConnection();
			}

			if (this.IsSharedCacheConnection)
			{
				return base.OpenConnection();
			}

			return this.connection ?? (this.connection = new SqlitePersistentDbConnection(base.OpenConnection()));
		}

		public override IDbConnection OpenServerConnection()
		{
			return this.OpenConnection();
		}

		private static readonly Regex IsSharedConnectionRegex = new Regex(@".*[^a-zA-Z]cache\s*\=\s*shared(([^a-zA-Z])|$).*", RegexOptions.Compiled | RegexOptions.IgnoreCase);
		private static readonly Regex IsMemoryConnectionRegex = new Regex(@"((file\:)?\:memory\:)|(.*[^a-zA-Z]mode\s*\=\s*memory(([^a-zA-Z])|$).*)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

		protected SqliteSqlDatabaseContext(DataAccessModel model, SqliteSqlDatabaseContextInfo contextInfo, SqlDataTypeProvider sqlDataTypeProvider, SqlQueryFormatterManager sqlQueryFormatterManager)
			: base(model, SqliteSqlDialect.Default, sqlDataTypeProvider, sqlQueryFormatterManager, Path.GetFileNameWithoutExtension(contextInfo.FileName), contextInfo)
		{
			this.FileName = contextInfo.FileName;
			this.IsSharedCacheConnection = IsSharedConnectionRegex.IsMatch(this.FileName);
			this.IsInMemoryConnection = IsMemoryConnectionRegex.IsMatch(this.FileName);
		}

		internal static bool IsRunningMono()
		{
			return Type.GetType("Mono.Runtime") != null;
		}

		public override SqlTransactionalCommandsContext CreateSqlTransactionalCommandsContext(Transaction transaction)
		{
			return new DefaultSqlTransactionalCommandsContext(this, transaction);
		}

		public override IDisabledForeignKeyCheckContext AcquireDisabledForeignKeyCheckContext(SqlTransactionalCommandsContext sqlDatabaseCommandsContext)
		{
			return new DisabledForeignKeyCheckContext(sqlDatabaseCommandsContext);	
		}

		public override void Dispose()
		{
			if (this.connection != null)
			{
				this.connection.Close();
			}

			base.Dispose();
		}
	}
}
