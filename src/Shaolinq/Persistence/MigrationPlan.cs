// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

 using System.Collections.Generic;
using System.Linq;

namespace Shaolinq.Persistence
{
	public class MigrationPlan
	{
		private readonly Dictionary<DataAccessModelDatabaseConnectionInfo, DatabaseMigrationPlan> migrationPlans;

		public MigrationPlan()
		{
			this.migrationPlans = new Dictionary<DataAccessModelDatabaseConnectionInfo, DatabaseMigrationPlan>();
		}

		public void AddDatabaseMigrationPlan(DataAccessModelDatabaseConnectionInfo contextInfo, DatabaseMigrationPlan databaseMigrationPlan)
		{
			this.migrationPlans[contextInfo] = databaseMigrationPlan;
		}

		public List<DataAccessModelDatabaseConnectionInfo> GetDatabaseConnectionInfos()
		{
			return this.migrationPlans.Keys.ToList();
		}

		public List<DatabaseMigrationPlan> GetDatabaseMigrationPlans()
		{
			return this.migrationPlans.Values.ToList();
		}

		public DatabaseMigrationPlan GetMigrationPlan(string databaseConnectionName)
		{
			foreach (var keyValue in migrationPlans)
			{
				if (keyValue.Key.ConnectionName == databaseConnectionName)
				{
					return keyValue.Value;
				}
			}

			return null;
		}
	}
}
