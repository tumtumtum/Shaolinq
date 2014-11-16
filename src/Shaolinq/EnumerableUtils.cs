using System.Collections.Generic;
using System.Linq;
using Platform.Collections;

namespace Shaolinq
{
	internal static class EnumerableUtils
	{
		public static ReadOnlyList<T> ToReadOnlyList<T>(this IEnumerable<T> enumerable)
		{
			if (enumerable == null)
			{
				return null;
			}

			if (enumerable is ReadOnlyList<T>)
			{
				return (ReadOnlyList<T>)enumerable;
			}

			return new ReadOnlyList<T>(enumerable.ToList());
		}
	}
}
