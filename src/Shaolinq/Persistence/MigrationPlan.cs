using System.Collections.Generic;
using System.Linq;

namespace Shaolinq.Persistence
{
	public class MigrationPlan
	{
		private readonly Dictionary<DataAccessModelPersistenceContextInfo, PersistenceContextMigrationPlan> migrationPlans;

		public MigrationPlan()
		{
			this.migrationPlans = new Dictionary<DataAccessModelPersistenceContextInfo, PersistenceContextMigrationPlan>();
		}

		public void AddPersistenceContextMigrationPlan(DataAccessModelPersistenceContextInfo contextInfo, PersistenceContextMigrationPlan persistenceContextMigrationPlan)
		{
			this.migrationPlans[contextInfo] = persistenceContextMigrationPlan;
		}

		public List<DataAccessModelPersistenceContextInfo> GetPersistenceContexts()
		{
			return this.migrationPlans.Keys.ToList();
		}

		public List<PersistenceContextMigrationPlan> GetPersistenceContextMigrationPlans()
		{
			return this.migrationPlans.Values.ToList();
		}

		public PersistenceContextMigrationPlan GetMigrationPlan(string persistenceContextName)
		{
			foreach (var keyValue in migrationPlans)
			{
				if (keyValue.Key.ContextName == persistenceContextName)
				{
					return keyValue.Value;
				}
			}

			return null;
		}
	}
}
