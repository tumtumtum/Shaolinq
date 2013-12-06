// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

 using Shaolinq.Persistence.Sql;

namespace Shaolinq.Persistence
{
	public abstract class MigrationPlanApplicator
	{
		private TypeDescriptorProvider typeDescriptorProvider;

		protected TypeDescriptorProvider TypeDescriptorProvider
		{
			get
			{
				if (this.typeDescriptorProvider == null)
				{
					this.typeDescriptorProvider = TypeDescriptorProvider.GetProvider(Model.DefinitionAssembly);
				}

				return this.typeDescriptorProvider;
			}
		}

		protected MigrationPlanApplicator(SystemDataBasedDatabaseConnection databaseConnection, DataAccessModel model)
		{
			this.Model = model;
			this.SystemDataBasedDatabaseConnection = databaseConnection;
			this.ModelTypeDescriptor = this.Model.ModelTypeDescriptor;
		}

		protected ModelTypeDescriptor ModelTypeDescriptor
		{
			get;
			set;
		}

		protected DataAccessModel Model
		{
			get;
			set;
		}

		protected DataAccessModelDatabaseConnectionInfo DatabaseConnectionInfo
		{
			get;
			set;
		}

		protected SystemDataBasedDatabaseConnection SystemDataBasedDatabaseConnection
		{
			get;
			set;
		}

		public abstract MigrationScripts CreateScripts(DatabaseMigrationPlan databaseMigrationPlan);
	}
}
