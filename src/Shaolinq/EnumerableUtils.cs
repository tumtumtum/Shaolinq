// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

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

			var list = enumerable as ReadOnlyList<T>;

			return list ?? new ReadOnlyList<T>(enumerable.ToList());
		}
	}
}
