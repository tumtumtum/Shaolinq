using System.Collections.Generic;
using Shaolinq.Persistence;

namespace Shaolinq
{
	public abstract class PersistenceContextProvider
	{
		public string ContextName { get; private set; }
		private readonly Dictionary<PersistenceMode, List<PersistenceContext>> databaseContextsByDatabaseMode = new Dictionary<PersistenceMode, List<PersistenceContext>>(PrimeNumbers.Prime29);

		protected PersistenceContextProvider(string contextName)
		{
			this.ContextName = contextName;
		}

		protected virtual void AddPersistenceContext(PersistenceMode persistenceMode, PersistenceContext persistenceContext)
		{
			List<PersistenceContext> contexts;

			if (!databaseContextsByDatabaseMode.TryGetValue(persistenceMode, out contexts))
			{
				contexts = new List<PersistenceContext>(16);

				databaseContextsByDatabaseMode[persistenceMode] = contexts;
			}

			contexts.Add(persistenceContext);
		}

		public virtual bool TryGetPersistenceContext(PersistenceMode persistenceMode, out PersistenceContext persistenceContext)
		{
			List<PersistenceContext> contexts;

			if (databaseContextsByDatabaseMode.TryGetValue(persistenceMode, out contexts))
			{
				persistenceContext = contexts[0];

				return true;
			}

			if (persistenceMode == PersistenceMode.ReadOnly)
			{
				if (databaseContextsByDatabaseMode.TryGetValue(PersistenceMode.ReadWrite, out contexts))
				{
					persistenceContext = contexts[0];

					return true;
				}
			}

			persistenceContext = null;

			return false;
		}
	}
}
