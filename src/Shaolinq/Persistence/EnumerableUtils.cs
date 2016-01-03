// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Shaolinq.Persistence
{
	public static class EnumerableUtils
	{
		public static IEnumerable<T?> DefaultIfEmptyCoalesceSpecifiedValue<T>(this IEnumerable<T?> enumerable, T? specifiedValue)
			where T : struct
		{
			using (var enumerator = enumerable.GetEnumerator())
			{
				if (!enumerator.MoveNext())
				{
					yield return specifiedValue ?? default(T);

					yield break;
				}

				yield return enumerator.Current;

				while (enumerator.MoveNext())
				{
					yield return enumerator.Current;
				}
			}
		}

		public static T SingleOrSpecifiedValueIfFirstIsDefaultValue<T>(this IEnumerable<T> enumerable, T specifiedValue)
		{
			var x = enumerable.Single();

			if (object.Equals(x, default(T)))
			{
				return specifiedValue;
			}

			return x;
		}

		public static T SingleOrExceptionIfFirstIsNull<T>(this IEnumerable<T?> enumerable)
			where T : struct
		{
			using (var enumerator = enumerable.GetEnumerator())
			{
				if (!enumerator.MoveNext() || enumerator.Current == null)
				{
					throw new InvalidOperationException("Sequence contains no elements");
				}

				return enumerator.Current.Value;
			}
		}

		public static IEnumerable<T> EmptyIfFirstIsNull<T>(this IEnumerable<T> enumerable)
		{
			using (var enumerator = enumerable.GetEnumerator())
			{
				if (!enumerator.MoveNext() || enumerator.Current == null)
				{
					yield break;
				}

				yield return enumerator.Current;
				
				while (enumerator.MoveNext())
				{
					yield return enumerator.Current;
				}
			}
		}

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
