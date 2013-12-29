using System;
using Shaolinq.Persistence;

namespace Shaolinq.MySql
{
	public class MySqlDatabaseCreator
		: DatabaseCreator
	{
		private readonly MySqlDatabaseConnection connection;

		public MySqlDatabaseCreator(MySqlDatabaseConnection connection, DataAccessModel model)
			: base(model)
		{
			this.connection = connection;
		}

		protected override bool CreateDatabaseOnly(bool overwrite)
		{
			var retval = false;
			var factory = this.connection.NewDbProviderFactory();

			using (var dbConnection = factory.CreateConnection())
			{
				dbConnection.ConnectionString = this.connection.databaselessConnectionString;

				dbConnection.Open();

				var command = dbConnection.CreateCommand();

				if (overwrite)
				{
					var drop = false;

					command.CommandText = String.Format("SHOW DATABASES;", this.connection.DatabaseName);

					using (var reader = command.ExecuteReader())
					{
						while (reader.Read())
						{
							var s = reader.GetString(0);

							if (s.Equals(this.connection.DatabaseName) ||
								s.Equals(this.connection.DatabaseName.ToLower()))
							{
								drop = true;

								break;
							}
						}
					}

					if (drop)
					{
						command.CommandText = String.Concat("DROP DATABASE ", this.connection.DatabaseName);
						command.ExecuteNonQuery();
					}

					command.CommandText = String.Concat("CREATE DATABASE ", this.connection.DatabaseName, "\nDEFAULT CHARACTER SET = utf8\nDEFAULT COLLATE = utf8_general_ci;");
					command.ExecuteNonQuery();

					retval = true;
				}
				else
				{
					try
					{
						command.CommandText = String.Concat("CREATE DATABASE ", this.connection.DatabaseName, "\nDEFAULT CHARACTER SET = utf8\nDEFAULT COLLATE = utf8_general_ci;");
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
