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

			var readOnlyCollection = enumerable as ReadOnlyCollection<T>;

			if (readOnlyCollection != null)
			{
				return readOnlyCollection; 				
			}

			var list = enumerable as List<T>;

			if (list != null)
			{
				return new ReadOnlyCollection<T>(list);
			}

			return new ReadOnlyCollection<T>(enumerable.ToList());
		}
	}
}
