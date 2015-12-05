// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

namespace Shaolinq
{
	public static class ReadOnlyListUtils
	{
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
