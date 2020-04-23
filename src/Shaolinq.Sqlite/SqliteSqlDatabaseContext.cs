// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Data;
using System.IO;
using System.Text.RegularExpressions;
using Shaolinq.Persistence;

namespace Shaolinq.Sqlite
{
	public abstract partial class SqliteSqlDatabaseContext
		: SqlDatabaseContext
	{
		public string FileName { get; protected set; }
		public bool IsInMemoryConnection { get; protected set; }
		public bool IsSharedCacheConnection { get; protected set; }
		
		private IDbConnection connection;

		[RewriteAsync]
		public override IDbConnection OpenConnection()
		{
			var retval = PrivateOpenConnection();

			if (retval == null)
			{
				return null;
			}

			using (var command = retval.CreateCommand())
			{
				command.CommandText = "PRAGMA foreign_keys = ON;";
				command.ExecuteNonQueryEx(this.DataAccessModel, true);
			}

			return retval;
		}

		[RewriteAsync]
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
			return OpenConnection();
		}

		private static readonly Regex IsSharedConnectionRegex = new Regex(@".*[^a-zA-Z]cache\s*\=\s*shared(([^a-zA-Z])|$).*", RegexOptions.Compiled | RegexOptions.IgnoreCase);
		private static readonly Regex IsMemoryConnectionRegex = new Regex(@"((file\:)?\:memory\:)|(.*[^a-zA-Z]mode\s*\=\s*memory(([^a-zA-Z])|$).*)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

		protected SqliteSqlDatabaseContext(DataAccessModel model, SqliteSqlDatabaseContextInfo contextInfo, SqlDataTypeProvider sqlDataTypeProvider, SqlQueryFormatterManager sqlQueryFormatterManager)
			: base(model, new SqliteSqlDialect(), sqlDataTypeProvider, sqlQueryFormatterManager, Path.GetFileNameWithoutExtension(contextInfo.FileName), contextInfo)
		{
			var connectionString = contextInfo.GetConnectionString();

			if (contextInfo.FileName == null && connectionString == null)
			{
				throw new ArgumentException($"Must supply {nameof(contextInfo.FileName)}, {nameof(contextInfo.ConnectionString)} or {nameof(contextInfo.ConnectionStringName)}", nameof(contextInfo));
			}

			this.FileName = contextInfo.FileName;

			this.IsSharedCacheConnection = IsSharedConnectionRegex.IsMatch(this.FileName ?? connectionString);
			this.IsInMemoryConnection = IsMemoryConnectionRegex.IsMatch(this.FileName ?? connectionString);
		}
		
		public override IDisabledForeignKeyCheckContext AcquireDisabledForeignKeyCheckContext(SqlTransactionalCommandsContext sqlDatabaseCommandsContext)
		{
			return new DisabledForeignKeyCheckContext(sqlDatabaseCommandsContext);	
		}

		public override void Dispose(bool disposing)
		{
			this.connection?.Close();
			base.Dispose(disposing);
		}
	}
}
