using System;
using System.Data.SQLite;
using System.IO;
using Shaolinq.Persistence;

namespace Shaolinq.Sqlite
{
	public class SqliteDatabaseCreator
		: DatabaseCreator
	{
		private readonly SqliteDatabaseConnection connection;
		
		public SqliteDatabaseCreator(SqliteDatabaseConnection connection, DataAccessModel model)
			: base(model)
		{
			this.connection = connection;
		}

		protected override bool CreateDatabaseOnly(bool overwrite)
		{
			var retval = false;
			var path = this.connection.DatabaseName;

			if (String.Equals(this.connection.DatabaseName, ":memory:", StringComparison.InvariantCultureIgnoreCase))
			{
				if (this.connection.inMemoryContext != null)
				{
					this.connection.inMemoryContext.RealDispose();

					this.connection.inMemoryContext = null;
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
