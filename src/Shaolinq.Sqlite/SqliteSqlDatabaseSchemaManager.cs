using System;
using System.IO;
using Shaolinq.Persistence;

namespace Shaolinq.Sqlite
{
	public abstract class SqliteSqlDatabaseSchemaManager
		: SqlDatabaseSchemaManager
	{
		protected SqliteSqlDatabaseSchemaManager(SqliteSqlDatabaseContext sqlDatabaseContext)
			: base(sqlDatabaseContext)
		{
		}

		protected abstract void CreateFile(string path);

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
