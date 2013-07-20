using System.Reflection;

namespace Shaolinq
{
	public static class PropertyInfoFastRef
	{
		public static readonly PropertyInfo DataAccessObjectInternalIsNewProperty = typeof(IDataAccessObject).GetProperty("IsNew", BindingFlags.Public | BindingFlags.Instance);
		public static readonly PropertyInfo DataAccessObjectInternalHasAutoIncrementKeyValueProperty = typeof(IDataAccessObject).GetProperty("HasAutoIncrementKeyValue", BindingFlags.Public | BindingFlags.Instance);
	}
}
