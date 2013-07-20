namespace Shaolinq.Persistence
{
	public abstract class PersistenceStoreCreator
	{
		public abstract bool CreatePersistenceStorage(bool overwrite);
	}
}
