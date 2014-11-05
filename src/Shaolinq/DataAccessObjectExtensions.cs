using System;
using Shaolinq.TypeBuilding;

namespace Shaolinq
{
	public static class DataAccessObjectExtensions
	{
		internal static T Include<T, U>(this T obj, Func<T, U> include)
			where U : DataAccessObject
		{
			throw new InvalidOperationException();
		}

		internal static IDataAccessObjectInternal ToObjectInternal(this DataAccessObject value)
		{
			// ReSharper disable SuspiciousTypeConversion.Global
			return (IDataAccessObjectInternal)value;
			// ReSharper restore SuspiciousTypeConversion.Global
		}
	}
}
