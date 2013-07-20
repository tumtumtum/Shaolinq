using System;

namespace Shaolinq
{
	internal static class TypeExtensions
	{
		public static bool IsDataAccessObjectType(this Type type)
		{
			return typeof(IDataAccessObject).IsAssignableFrom(type);
		}
	}
}
