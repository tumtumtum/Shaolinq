// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

﻿using System;
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
	public class SqlitePersistenceContext
		: SqlPersistenceContext
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
		
		public SqlitePersistenceContext(string fileName, string schemaNamePrefix)
			: base(fileName, SqliteSqlDialect.Default, SqliteSqlDataTypeProvider.Instance)
		{
			this.SchemaNamePrefix = EnvironmentSubstitutor.Substitute(schemaNamePrefix);
			connectionString = "Data Source=" + this.PersistenceStoreName + ";foreign keys=True";
		}

		private SqliteSqlPersistenceTransactionContext inMemoryContext;

		public override PersistenceTransactionContext NewDataTransactionContext(BaseDataAccessModel dataAccessModel, Transaction transaction)
		{
			if (String.Equals(this.PersistenceStoreName, ":memory:", StringComparison.InvariantCultureIgnoreCase))
			{
				if (inMemoryContext == null)
				{
					inMemoryContext = new SqliteSqlPersistenceTransactionContext(this, dataAccessModel, transaction);
				}

				return inMemoryContext;
			}

			return new SqliteSqlPersistenceTransactionContext(this, dataAccessModel, transaction);
		}

		public override Sql92QueryFormatter NewQueryFormatter(BaseDataAccessModel dataAccessModel, SqlDataTypeProvider sqlDataTypeProvider, SqlDialect sqlDialect, Expression expression, SqlQueryFormatterOptions options)
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

		public override SqlSchemaWriter NewSqlSchemaWriter(BaseDataAccessModel model, DataAccessModelPersistenceContextInfo persistenceContextInfo)
		{
			return new SqlSchemaWriter(this, model, persistenceContextInfo);
		}

		public override PersistenceStoreCreator NewPersistenceStoreCreator(BaseDataAccessModel model, DataAccessModelPersistenceContextInfo persistenceContextInfo)
		{
			return new SqliteSqlDatabaseCreator(this, model, persistenceContextInfo);
		}

		public override MigrationPlanApplicator NewMigrationPlanApplicator(BaseDataAccessModel model, DataAccessModelPersistenceContextInfo dataAccessModelPersistenceContextInfo)
		{
			throw new NotImplementedException();
		}

		public override MigrationPlanCreator NewMigrationPlanCreator(BaseDataAccessModel model, DataAccessModelPersistenceContextInfo dataAccessModelPersistenceContextInfo)
		{
			return new SqlPersistenceContextMigrationPlanCreator(this, model, dataAccessModelPersistenceContextInfo);
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
        
		public override IDisabledForeignKeyCheckContext AcquireDisabledForeignKeyCheckContext(PersistenceTransactionContext persistenceTransactionContext)
		{
			return new DisabledForeignKeyCheckContext(persistenceTransactionContext);	
		}

		public override IPersistenceQueryProvider NewQueryProvider(BaseDataAccessModel dataAccessModel, PersistenceContext persistenceContext)
		{
			return new SqlQueryProvider(dataAccessModel, persistenceContext);
		}

		public override void DropAllConnections()
		{
		}
	}
}
