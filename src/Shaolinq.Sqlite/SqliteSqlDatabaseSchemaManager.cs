// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Data;
using System.IO;
using System.Linq.Expressions;
using Shaolinq.Persistence;

namespace Shaolinq.Sqlite
{
	public abstract partial class SqliteSqlDatabaseSchemaManager
		: SqlDatabaseSchemaManager
	{
		// We keep a reference to inMemoryConnection around

		private IDbConnection inMemoryConnection;

		protected SqliteSqlDatabaseSchemaManager(SqliteSqlDatabaseContext sqlDatabaseContext)
			: base(sqlDatabaseContext)
		{
		}

		public override void Dispose()
		{
			if (this.inMemoryConnection != null)
			{
				try
				{
					this.inMemoryConnection.Close();
				}
				catch (ObjectDisposedException)
				{	
				}

				this.inMemoryConnection = null;
			}
		}

		protected abstract void CreateFile(string path);

		[RewriteAsync]
		protected override bool CreateDatabaseOnly(Expression dataDefinitionExpressions, DatabaseCreationOptions options)
		{
			var retval = false;
			var sqliteSqlDatabaseContext = (SqliteSqlDatabaseContext)this.SqlDatabaseContext;
			var overwrite = options == DatabaseCreationOptions.DeleteExistingDatabase;

			var path = sqliteSqlDatabaseContext.FileName;

			if (sqliteSqlDatabaseContext.IsInMemoryConnection)
			{
				if (overwrite)
				{
					var connection = sqliteSqlDatabaseContext.OpenConnection();

					if (sqliteSqlDatabaseContext.IsSharedCacheConnection)
					{
						// Keeping a reference around so that the in-memory DB survives 
						this.inMemoryConnection = sqliteSqlDatabaseContext.OpenConnection();
					}

					using (var command = connection.CreateCommand())
					{
						command.CommandText =
						@"
							PRAGMA writable_schema = 1;
							delete from sqlite_master where type = 'table';
							PRAGMA writable_schema = 0;
							VACUUM;
						";

                        command.ExecuteNonQueryEx(this.SqlDatabaseContext.DataAccessModel, true);
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
