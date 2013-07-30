using System;

namespace Shaolinq
{
	internal static class TypeExtensions
	{
		public static bool IsNullableType(this Type type)
		{
			return Nullable.GetUnderlyingType(type) != null;
		}

		public static bool IsDataAccessObjectType(this Type type)
		{
			return typeof(IDataAccessObject).IsAssignableFrom(type);
		}

		public static Type NonNullableType(this Type type)
		{
			var underlying = Nullable.GetUnderlyingType(type);

			if (underlying != null)
			{
				return underlying;
			}

			return type;
		}
	}
}
