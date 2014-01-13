// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Data;
using System.Data.Common;
using System.Data.SQLite;
using System.IO;
using System.Text.RegularExpressions;
using System.Transactions;
﻿using Shaolinq.Persistence;

namespace Shaolinq.Sqlite
{
	public abstract class SqliteSqlDatabaseContext
		: SqlDatabaseContext
	{
		public string FileName { get; private set; }
		public bool IsInMemoryConnection { get; private set; }
		public bool IsSharedCacheConnection { get; private set; }
		
		private IDbConnection connection;

		public override IDbConnection OpenConnection()
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

			var connectionStringBuilder = new SQLiteConnectionStringBuilder()
			{
				Enlist = false,
				ForeignKeys = true,
				FullUri = contextInfo.FileName
			};

			this.ConnectionString = connectionStringBuilder.ConnectionString;
			this.ServerConnectionString = this.ConnectionString;

			if (SqliteSqlDatabaseContext.IsRunningMono())
			{
				this.SchemaManager = new SqliteMonoSqlDatabaseSchemaManager(this);
			}
			else
			{
				this.SchemaManager = new SqliteWindowsSqlDatabaseSchemaManager(this);
			}
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
