// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Shaolinq.Persistence
{
	public static class EnumerableUtils
	{
		public static ReadOnlyCollection<T> ToReadOnlyCollection<T>(this IEnumerable<T> enumerable)
		{
			if (enumerable == null)
			{
				return null;
			}

			var list = enumerable as ReadOnlyCollection<T>;

			return list ?? new ReadOnlyCollection<T>(enumerable.ToList());
		}
	}
}
