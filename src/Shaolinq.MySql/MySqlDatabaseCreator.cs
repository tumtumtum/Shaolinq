using System;
using Shaolinq.Persistence;

namespace Shaolinq.MySql
{
	public class MySqlDatabaseCreator
		: DatabaseCreator
	{
		private readonly MySqlSqlDatabaseContext sqlDatabaseContext;

		public MySqlDatabaseCreator(MySqlSqlDatabaseContext sqlDatabaseContext, DataAccessModel model)
			: base(model)
		{
			this.sqlDatabaseContext = sqlDatabaseContext;
		}

		protected override bool CreateDatabaseOnly(bool overwrite)
		{
			var retval = false;
			var factory = this.sqlDatabaseContext.CreateDbProviderFactory();

			using (var dbConnection = factory.CreateConnection())
			{
				dbConnection.ConnectionString = this.sqlDatabaseContext.databaselessConnectionString;

				dbConnection.Open();

				var command = dbConnection.CreateCommand();

				if (overwrite)
				{
					var drop = false;

					command.CommandText = String.Format("SHOW DATABASES;", this.sqlDatabaseContext.DatabaseName);

					using (var reader = command.ExecuteReader())
					{
						while (reader.Read())
						{
							var s = reader.GetString(0);

							if (s.Equals(this.sqlDatabaseContext.DatabaseName) ||
								s.Equals(this.sqlDatabaseContext.DatabaseName.ToLower()))
							{
								drop = true;

								break;
							}
						}
					}

					if (drop)
					{
						command.CommandText = String.Concat("DROP DATABASE ", this.sqlDatabaseContext.DatabaseName);
						command.ExecuteNonQuery();
					}

					command.CommandText = String.Concat("CREATE DATABASE ", this.sqlDatabaseContext.DatabaseName, "\nDEFAULT CHARACTER SET = utf8\nDEFAULT COLLATE = utf8_general_ci;");
					command.ExecuteNonQuery();

					retval = true;
				}
				else
				{
					try
					{
						command.CommandText = String.Concat("CREATE DATABASE ", this.sqlDatabaseContext.DatabaseName, "\nDEFAULT CHARACTER SET = utf8\nDEFAULT COLLATE = utf8_general_ci;");
						command.ExecuteNonQuery();

						retval = true;
					}
					catch
					{
						retval = false;
					}
				}
			}

			return retval;
		}
	}
}
