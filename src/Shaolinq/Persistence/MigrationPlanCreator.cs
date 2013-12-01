// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

 using Shaolinq.Persistence.Sql;

namespace Shaolinq.Persistence
{
	public abstract class MigrationPlanCreator
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

		protected MigrationPlanCreator(SqlPersistenceContext sqlPersistenceContext, DataAccessModel model, DataAccessModelPersistenceContextInfo persistenceContextInfo)
		{
			this.Model = model;
			this.SqlPersistenceContext = sqlPersistenceContext;
			this.PersistenceContextInfo = persistenceContextInfo;
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

		protected DataAccessModelPersistenceContextInfo PersistenceContextInfo
		{
			get;
			set;
		}

		protected SqlPersistenceContext SqlPersistenceContext
		{
			get;
			set;
		}

		public abstract PersistenceContextMigrationPlan CreateMigrationPlan();
	}
}
