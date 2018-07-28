// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;

namespace Shaolinq
{
	public static class ReadOnlyListUtils
	{
		public static int FindIndex<T>(this IReadOnlyList<T> list, Predicate<T> match)
		{
			for (var i = 0; i < list.Count; i++)
			{
				if (match(list[i]))
				{
					return i;
				}
			}

			return -1;
		}

		public static int IndexOf<T>(this IReadOnlyList<T> list, T value)
		{
			for (var i = 0; i < list.Count; i++)
			{
				if (list[i].Equals(value))
				{
					return i;
				}
			}

			return -1;
		}
	}
}
