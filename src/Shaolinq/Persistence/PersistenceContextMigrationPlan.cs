using System.Collections.Generic;

namespace Shaolinq.Persistence
{
	public class PersistenceContextMigrationPlan
	{
		public List<MigrationTypeInfo> NewTypes
		{
			get;
			set;
		}

		public List<MigrationTypeInfo> DeletedTypes
		{
			get;
			set;
		}

		public List<MigrationTypeInfo> ModifiedTypes
		{
			get;
			set;
		}

		public PersistenceContextMigrationPlan()
		{
			this.NewTypes = new List<MigrationTypeInfo>();
			this.ModifiedTypes = new List<MigrationTypeInfo>();
			this.DeletedTypes = new List<MigrationTypeInfo>();
		}
	}
}
