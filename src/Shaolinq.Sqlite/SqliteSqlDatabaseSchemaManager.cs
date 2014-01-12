using System;
using System.Data.SQLite;
using System.IO;
using Shaolinq.Persistence;

namespace Shaolinq.Sqlite
{
	public class SqliteSqlDatabaseSchemaManager
		: SqlDatabaseSchemaManager
	{
		public SqliteSqlDatabaseSchemaManager(SqliteSqlDatabaseContext sqlDatabaseContext)
			: base(sqlDatabaseContext)
		{
		}

		protected override bool CreateDatabaseOnly(bool overwrite)
		{
			var retval = false;
			var sqliteSqlDatabaseContext = (SqliteSqlDatabaseContext)this.sqlDatabaseContext;

			var path = sqliteSqlDatabaseContext.FileName;

			if (String.Equals(sqliteSqlDatabaseContext.FileName, ":memory:", StringComparison.InvariantCultureIgnoreCase))
			{
				if (sqliteSqlDatabaseContext.inMemoryContext != null)
				{
					sqliteSqlDatabaseContext.inMemoryContext.RealDispose();

					sqliteSqlDatabaseContext.inMemoryContext = null;
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

				for (int i = 0; i < 2; i++)
				{
					try
					{
						SQLiteConnection.CreateFile(path);

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
							SQLiteConnection.CreateFile(path);

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
