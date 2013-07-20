using Shaolinq.Persistence;

namespace Shaolinq
{
	public static class DataAccessObjectExtensions
	{
		public static PersistenceContext GetPersistenceContext(this IDataAccessObject dataAccessObject)
		{
			return dataAccessObject.DataAccessModel.GetPersistenceContext(dataAccessObject);
		}
	}
}
