// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;

namespace Shaolinq
{
	internal static class InternalListExtensions
	{
		public static IList<T> FastWhere<T>(this IList<T> list, Predicate<T> accept)
		{
			List<T> newList = null;

			for (var i = 0; i < list.Count; i++)
			{
				if (!accept(list[i]))
				{
					if (newList == null)
					{
						newList = new List<T>(list.Count - 1);

						for (var j = 0; j < i; j++)
						{
							newList.Add(list[j]);
						}
					}
				}
				else
				{
					newList?.Add(list[i]);
				}
			}

			return newList ?? list;
		}
	}
}
