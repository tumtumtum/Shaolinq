namespace Shaolinq
{
	public class DataAccessObjects<T>
		: DataAccessObjectsQueryable<T>
		where T : class, IDataAccessObject
	{
	}
}
