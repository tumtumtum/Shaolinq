// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Data.Common;
using System.Data.SQLite;
using System.IO;
using System.Linq.Expressions;
using System.Transactions;
﻿using Shaolinq.Persistence;
﻿using Shaolinq.Persistence.Sql;
﻿using Shaolinq.Persistence.Sql.Linq;

namespace Shaolinq.Sqlite
{
	public class SqliteDatabaseConnection
		: SystemDataBasedDatabaseConnection
	{
		public override bool SupportsNestedTransactions
		{
			get
			{
				return false;
			}
		}

		protected override string GetConnectionString()
		{
			return connectionString;
		}

		public override bool SupportsDisabledForeignKeyCheckContext
		{
			get
			{
				return true;
			}
		}

		private readonly string connectionString;
		
		public SqliteDatabaseConnection(string fileName, string schemaNamePrefix)
			: base(fileName, SqliteSqlDialect.Default, SqliteSqlDataTypeProvider.Instance)
		{
			this.SchemaNamePrefix = EnvironmentSubstitutor.Substitute(schemaNamePrefix);
			connectionString = "Data Source=" + this.PersistenceStoreName + ";foreign keys=True";
		}

		private SqliteSqlDatabaseTransactionContext inMemoryContext;

		public override DatabaseTransactionContext NewDataTransactionContext(DataAccessModel dataAccessModel, Transaction transaction)
		{
			if (String.Equals(this.PersistenceStoreName, ":memory:", StringComparison.InvariantCultureIgnoreCase))
			{
				if (inMemoryContext == null)
				{
					inMemoryContext = new SqliteSqlDatabaseTransactionContext(this, dataAccessModel, transaction);
				}

				return inMemoryContext;
			}

			return new SqliteSqlDatabaseTransactionContext(this, dataAccessModel, transaction);
		}

		public override Sql92QueryFormatter NewQueryFormatter(DataAccessModel dataAccessModel, SqlDataTypeProvider sqlDataTypeProvider, SqlDialect sqlDialect, Expression expression, SqlQueryFormatterOptions options)
		{
			return new SqliteSqlQueryFormatter(dataAccessModel, sqlDataTypeProvider, sqlDialect, expression, options);
		}

		protected override DbProviderFactory NewDbProproviderFactory()
		{
			return new SQLiteFactory();
		}

		public override TableDescriptor GetTableDescriptor(string tableName)
		{
			throw new NotImplementedException();
		}

		public override SqlSchemaWriter NewSqlSchemaWriter(DataAccessModel model)
		{
			return new SqlSchemaWriter(this, model);
		}

		public override DatabaseCreator NewDatabaseCreator(DataAccessModel model)
		{
			return new SqliteSqlDatabaseCreator(this, model);
		}

		public override MigrationPlanApplicator NewMigrationPlanApplicator(DataAccessModel model)
		{
			throw new NotImplementedException();
		}

		public override MigrationPlanCreator NewMigrationPlanCreator(DataAccessModel model)
		{
			return new SqlDatabaseMigrationPlanCreator(this, model);
		}

		public override bool CreateDatabase(bool overwrite)
		{
			var retval = false;
			var path = this.PersistenceStoreName;

			if (String.Equals(this.PersistenceStoreName, ":memory:", StringComparison.InvariantCultureIgnoreCase))
			{
				if (inMemoryContext != null)
				{
					inMemoryContext.RealDispose();

					inMemoryContext = null;
				}
				
				return true;
			}
			
			if (overwrite)
			{
				try
				{
					File.Delete(path);
				}
				catch(FileNotFoundException)
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
        
		public override IDisabledForeignKeyCheckContext AcquireDisabledForeignKeyCheckContext(DatabaseTransactionContext databaseTransactionContext)
		{
			return new DisabledForeignKeyCheckContext(databaseTransactionContext);	
		}

		public override IPersistenceQueryProvider NewQueryProvider(DataAccessModel dataAccessModel, DatabaseConnection databaseConnection)
		{
			return new SqlQueryProvider(dataAccessModel, databaseConnection);
		}

		public override void DropAllConnections()
		{
		}
	}
}
