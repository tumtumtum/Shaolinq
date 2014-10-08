using System;

namespace Shaolinq
{
	public static class DataAccessObjectExtensions
	{
		public static T Include<T, U>(this T queryable, Func<T, U> include)
		{
			return queryable;
		}
	}
}
