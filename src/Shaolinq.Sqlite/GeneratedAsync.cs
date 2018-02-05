namespace Shaolinq.Sqlite
{
#pragma warning disable
	using System;
	using System.IO;
	using System.Data;
	using System.Threading;
	using System.Threading.Tasks;
	using System.Linq.Expressions;
	using Shaolinq;
	using Shaolinq.Sqlite;
	using Shaolinq.Persistence;

	public abstract partial class SqliteSqlDatabaseSchemaManager
	{
		protected override Task<bool> CreateDatabaseOnlyAsync(Expression dataDefinitionExpressions, DatabaseCreationOptions options)
		{
			return this.CreateDatabaseOnlyAsync(dataDefinitionExpressions, options, CancellationToken.None);
		}

		protected override async Task<bool> CreateDatabaseOnlyAsync(Expression dataDefinitionExpressions, DatabaseCreationOptions options, CancellationToken cancellationToken)
		{
			var retval = false;
			var sqliteSqlDatabaseContext = (SqliteSqlDatabaseContext)this.SqlDatabaseContext;
			var overwrite = options == DatabaseCreationOptions.DeleteExistingDatabase;
			var path = sqliteSqlDatabaseContext.FileName;
			if (sqliteSqlDatabaseContext.IsInMemoryConnection)
			{
				if (overwrite)
				{
					var connection = (await sqliteSqlDatabaseContext.OpenConnectionAsync(cancellationToken).ConfigureAwait(false));
					if (sqliteSqlDatabaseContext.IsSharedCacheConnection)
					{
						// Keeping a reference around so that the in-memory DB survives 
						this.inMemoryConnection = (await sqliteSqlDatabaseContext.OpenConnectionAsync(cancellationToken).ConfigureAwait(false));
					}

					using (var command = connection.CreateCommand())
					{
						command.CommandText = @"
							PRAGMA writable_schema = 1;
							delete from sqlite_master where type = 'table';
							PRAGMA writable_schema = 0;
							VACUUM;
						";
						await command.ExecuteNonQueryExAsync(this.SqlDatabaseContext.DataAccessModel, cancellationToken, true).ConfigureAwait(false);
					}
				}

				return true;
			}

			if (overwrite)
			{
				try
				{
					File.Delete(path);
				}
				catch (FileNotFoundException)
				{
				}
				catch (DirectoryNotFoundException)
				{
				}

				for (var i = 0; i < 2; i++)
				{
					try
					{
						this.CreateFile(path);
						break;
					}
					catch (FileNotFoundException)
					{
					}
					catch (DirectoryNotFoundException)
					{
					}

					var directoryPath = Path.GetDirectoryName(path);
					if (!String.IsNullOrEmpty(directoryPath))
					{
						try
						{
							Directory.CreateDirectory(directoryPath);
						}
						catch
						{
						}
					}
				}

				retval = true;
			}
			else
			{
				if (!File.Exists(path))
				{
					for (var i = 0; i < 2; i++)
					{
						try
						{
							this.CreateFile(path);
							break;
						}
						catch (FileNotFoundException)
						{
						}
						catch (DirectoryNotFoundException)
						{
						}

						var directoryPath = Path.GetDirectoryName(path);
						if (!String.IsNullOrEmpty(directoryPath))
						{
							try
							{
								Directory.CreateDirectory(directoryPath);
							}
							catch
							{
							}
						}
					}

					retval = true;
				}
				else
				{
					retval = false;
				}
			}

			return retval;
		}
	}
}

namespace Shaolinq.Sqlite
{
#pragma warning disable
	using System;
	using System.IO;
	using System.Data;
	using System.Threading;
	using System.Threading.Tasks;
	using System.Text.RegularExpressions;
	using Shaolinq;
	using Shaolinq.Sqlite;
	using Shaolinq.Persistence;

	public abstract partial class SqliteSqlDatabaseContext
	{
		public override Task<IDbConnection> OpenConnectionAsync()
		{
			return this.OpenConnectionAsync(CancellationToken.None);
		}

		public override async Task<IDbConnection> OpenConnectionAsync(CancellationToken cancellationToken)
		{
			var retval = (await this.PrivateOpenConnectionAsync(cancellationToken).ConfigureAwait(false));
			if (retval == null)
			{
				return null;
			}

			using (var command = retval.CreateCommand())
			{
				command.CommandText = "PRAGMA foreign_keys = ON;";
				await command.ExecuteNonQueryExAsync(this.DataAccessModel, cancellationToken, true).ConfigureAwait(false);
			}

			return retval;
		}

		private Task<IDbConnection> PrivateOpenConnectionAsync()
		{
			return this.PrivateOpenConnectionAsync(CancellationToken.None);
		}

		private async Task<IDbConnection> PrivateOpenConnectionAsync(CancellationToken cancellationToken)
		{
			if (!this.IsInMemoryConnection)
			{
				return (await base.OpenConnectionAsync(cancellationToken).ConfigureAwait(false));
			}

			if (this.IsSharedCacheConnection)
			{
				return (await base.OpenConnectionAsync(cancellationToken).ConfigureAwait(false));
			}

			return this.connection ?? (this.connection = new SqlitePersistentDbConnection((await base.OpenConnectionAsync(cancellationToken).ConfigureAwait(false))));
		}
	}
}

namespace Shaolinq.Sqlite
{
#pragma warning disable
	using System;
	using System.Data;
	using System.Linq;
	using System.Threading;
	using System.Data.Common;
	using System.Data.SQLite;
	using System.Threading.Tasks;
	using System.Collections.Generic;
	using System.Text.RegularExpressions;
	using Shaolinq;
	using Shaolinq.Sqlite;
	using Shaolinq.Logging;
	using Shaolinq.Persistence;

	public partial class SqliteOfficialSqlDatabaseContext
	{
		public override Task BackupAsync(SqlDatabaseContext sqlDatabaseContext)
		{
			return this.BackupAsync(sqlDatabaseContext, CancellationToken.None);
		}

		public override async Task BackupAsync(SqlDatabaseContext sqlDatabaseContext, CancellationToken cancellationToken)
		{
			if (!(sqlDatabaseContext is SqliteOfficialSqlDatabaseContext))
			{
				throw new ArgumentException($"Needs to be a {nameof(SqliteOfficialSqlDatabaseContext)}", nameof(sqlDatabaseContext));
			}

			using (var connection = (await this.OpenConnectionAsync(cancellationToken).ConfigureAwait(false)))
			{
				using (var otherConnection = (await sqlDatabaseContext.OpenConnectionAsync(cancellationToken).ConfigureAwait(false)))
				{
					this.GetSqliteConnection(connection).BackupDatabase(this.GetSqliteConnection(otherConnection), "main", "main", -1, null, 1000);
				}
			}
		}
	}
}