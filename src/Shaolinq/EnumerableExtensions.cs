// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Shaolinq.Persistence;

namespace Shaolinq
{
	public static partial class EnumerableExtensions
	{
		[RewriteAsync]
		internal static T AlwaysReadFirst<T>(this IEnumerable<T> source)
		{
			return source.First();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static IEnumerable<T?> DefaultIfEmptyCoalesceSpecifiedValue<T>(this IEnumerable<T?> source, T? specifiedValue)
			where T : struct => source.DefaultIfEmptyCoalesceSpecifiedValueAsync(specifiedValue);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static IAsyncEnumerable<T?> DefaultIfEmptyCoalesceSpecifiedValueAsync<T>(this IEnumerable<T?> source, T? specifiedValue)
			where T : struct => new AsyncEnumerableAdapter<T?>(() => new DefaultIfEmptyCoalesceSpecifiedValueEnumerator<T>(source.GetAsyncEnumeratorOrAdapt(), specifiedValue));

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static IAsyncEnumerable<T> DefaultIfEmptyAsync<T>(this IEnumerable<T> source)
			=> source.DefaultIfEmptyAsync(default(T));

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static IAsyncEnumerable<T> DefaultIfEmptyAsync<T>(this IEnumerable<T> source, T defaultValue)
			=> new AsyncEnumerableAdapter<T>(() => new DefaultIfEmptyEnumerator<T>(source.GetAsyncEnumeratorOrAdapt(), defaultValue));

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static IEnumerable<T> EmptyIfFirstIsNull<T>(this IEnumerable<T> source) 
			=> new AsyncEnumerableAdapter<T>(() => new EmptyIfFirstIsNullEnumerator<T>(source.GetAsyncEnumeratorOrAdapt()));

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static IAsyncEnumerable<T> EmptyIfFirstIsNullAsync<T>(this IEnumerable<T> source, CancellationToken cancellationToken) 
			=> new AsyncEnumerableAdapter<T>(() => new EmptyIfFirstIsNullEnumerator<T>(source.GetAsyncEnumeratorOrAdapt()));
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static Task<bool> MoveNextAsync<T>(this IAsyncEnumerator<T> enumerator)
			=> enumerator.MoveNextAsync(CancellationToken.None);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static Task<bool> MoveNextAsync<T>(this IAsyncEnumerator<T> enumerator, CancellationToken cancellationToken)
			=> enumerator.MoveNextAsync(cancellationToken);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static IAsyncEnumerable<T> GetAsyncEnumerableOrAdapt<T>(this IEnumerable<T> source)
			=> (source as IAsyncEnumerable<T>) ?? new AsyncEnumerableAdapter<T>(source.GetAsyncEnumeratorOrAdapt);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static IAsyncEnumerator<T> GetAsyncEnumeratorOrAdapt<T>(this IEnumerable<T> source)
			=> (source as IAsyncEnumerable<T>)?.GetAsyncEnumerator() ?? new AsyncEnumeratorAdapter<T>(source.GetEnumerator());

		internal static IAsyncEnumerator<T> GetAsyncEnumeratorOrThrow<T>(this IEnumerable<T> source)
		{
			if (!(source is IAsyncEnumerable<T> asyncEnumerable))
			{
				throw new NotSupportedException($"The given enumerable {source.GetType().Name} does not support {nameof(IAsyncEnumerable<T>)}");
			}

			return asyncEnumerable.GetAsyncEnumerator();
		}

		[RewriteAsync(MethodAttributes.Public)]
		private static int Count<T>(this IEnumerable<T> source)
		{
			if (source is ICollection<T> list)
			{
				return list.Count;
			}

			var retval = 0;

			using (var enumerator = source.GetAsyncEnumeratorOrAdapt())
			{
				while (enumerator.MoveNext())
				{
					retval++;
				}
			}

			return retval;
		}

		[RewriteAsync(MethodAttributes.Public)]
		private static long LongCount<T>(this IEnumerable<T> source)
		{
			if (source is ICollection<T> list)
			{
				return list.Count;
			}

			var retval = 0L;

			using (var enumerator = source.GetAsyncEnumeratorOrAdapt())
			{
				while (enumerator.MoveNext())
				{
					retval++;
				}
			}

			return retval;
		}

		[RewriteAsync]
		internal static T SingleOrSpecifiedValueIfFirstIsDefaultValue<T>(this IEnumerable<T> source, T specifiedValue)
		{
			using (var enumerator = source.GetAsyncEnumeratorOrAdapt())
			{
				if (!enumerator.MoveNext())
				{
					return Enumerable.Single<T>(Enumerable.Empty<T>());
				}

				var result = enumerator.Current;

				if (enumerator.MoveNext())
				{
					return Enumerable.Single<T>(new T[2]);
				}

				if (Equals(result, default(T)))
				{
					return specifiedValue;
				}

				return result;
			}
		}

		[RewriteAsync(MethodAttributes.Public)]
		private static T Single<T>(this IEnumerable<T> source)
		{
			using (var enumerator = source.GetAsyncEnumeratorOrAdapt())
			{
				if (!enumerator.MoveNext())
				{
					return Enumerable.Single<T>(Enumerable.Empty<T>());
				}

				var result = enumerator.Current;

				if (enumerator.MoveNext())
				{
					return Enumerable.Single<T>(new T[2]);
				}

				return result;
			}
		}

		[RewriteAsync(MethodAttributes.Public)]
		private static T SingleOrDefault<T>(this IEnumerable<T> source)
		{
			using (var enumerator = source.GetAsyncEnumeratorOrAdapt())
			{
				if (!enumerator.MoveNext())
				{
					return default(T);
				}

				var result = enumerator.Current;

				if (enumerator.MoveNext())
				{
					return Enumerable.Single(new T[2]);
				}

				return result;
			}
		}

		[RewriteAsync(MethodAttributes.Public)]
		private static T First<T>(this IEnumerable<T> enumerable)
		{
			using (var enumerator = enumerable.GetAsyncEnumeratorOrAdapt())
			{
				if (!enumerator.MoveNext())
				{
					return Enumerable.First(Enumerable.Empty<T>());
				}

				return enumerator.Current;
			}
		}

		[RewriteAsync(MethodAttributes.Public)]
		private static T FirstOrDefault<T>(this IEnumerable<T> source)
		{
			using (var enumerator = source.GetAsyncEnumeratorOrAdapt())
			{
				if (!enumerator.MoveNext())
				{
					return default(T);
				}

				return enumerator.Current;
			}
		}

		[RewriteAsync]
		internal static T SingleOrExceptionIfFirstIsNull<T>(this IEnumerable<T?> source)
			where T : struct
		{
			using (var enumerator = source.GetAsyncEnumeratorOrAdapt())
			{
				if (!enumerator.MoveNext() || enumerator.Current == null)
				{
					throw new InvalidOperationException("Sequence contains no elements");
				}

				return enumerator.Current.Value;
			}
		}

		public static void WithEach<T>(this IEnumerable<T> source, Action<T> value)
		{
			using (var enumerator = source.GetAsyncEnumeratorOrAdapt())
			{
				while (enumerator.MoveNext())
				{
					value(enumerator.Current);
				}
			}
		}

		public static void WithEach<T>(this IEnumerable<T> source, Func<T, bool> value)
		{
			using (var enumerator = source.GetAsyncEnumeratorOrAdapt())
			{
				while (enumerator.MoveNext())
				{
					if (!value(enumerator.Current))
					{
						break;
					}
				}
			}
		}

		public static Task WhenAll<T>(this IEnumerable<T> source, Func<T, Task> tasker)
		{
			var tasks = new List<Task>();

			using (var enumerator = source.GetAsyncEnumeratorOrAdapt())
			{
				while (enumerator.MoveNext())
				{
					tasks.Add(tasker(enumerator.Current));
				}
			}

			return Task.WhenAll(tasks.ToArray());
		}

		public static Task WhenAll<T>(this IEnumerable<T> source, Func<T, Task<T>> tasker)
		{
			var tasks = new List<Task>();

			using (var enumerator = source.GetAsyncEnumeratorOrAdapt())
			{
				while (enumerator.MoveNext())
				{
					tasks.Add(tasker(enumerator.Current));
				}
			}

			return Task.WhenAll(tasks.ToArray());
		}
		
		public static ReadOnlyCollection<T> ToReadOnlyCollection<T>(this IEnumerable<T> source)
		{
			if (source == null)
			{
				return null;
			}

			var readonlyCollection = source as ReadOnlyCollection<T>;

			if (readonlyCollection != null)
			{
				return readonlyCollection;
			}

			var list = source as IList<T>;

			if (list != null)
			{
				return new ReadOnlyCollection<T>(list);
			}

			// ReSharper disable once SuspiciousTypeConversion.Global
			var collection = source as ICollection<T>;

			var retval = collection == null ? new List<T>() : new List<T>(collection.Count);

			using (var enumerator = source.GetAsyncEnumeratorOrAdapt())
			{
				while (enumerator.MoveNext())
				{
					retval.Add(enumerator.Current);
				}
			}

			return new ReadOnlyCollection<T>(retval);
		}

		public static Task<List<T>> ToListAsync<T>(this IEnumerable<T> source)
		{
			return source.ToListAsync(CancellationToken.None);
		}

		public static async Task<List<T>> ToListAsync<T>(this IEnumerable<T> source, CancellationToken cancellationToken)
		{
			// ReSharper disable once SuspiciousTypeConversion.Global

			if (!(source is ReusableQueryable<T> queryable))
			{
				return source.ToList();
			}

			// ReSharper disable once SuspiciousTypeConversion.Global

			var result = !(source is ICollection<T> collection) ? new List<T>() : new List<T>(collection.Count);

			using (var enumerator = queryable.GetAsyncEnumerator())
			{
				while (await enumerator.MoveNextAsync(cancellationToken))
				{
					cancellationToken.ThrowIfCancellationRequested();

					result.Add(enumerator.Current);
				}
			}

			return result;
		}

		public static Task<ReadOnlyCollection<T>> ToReadOnlyCollectionAsync<T>(this IEnumerable<T> source)
		{
			return source.ToReadOnlyCollectionAsync(CancellationToken.None);
		}

		public static async Task<ReadOnlyCollection<T>> ToReadOnlyCollectionAsync<T>(this IEnumerable<T> source, CancellationToken cancellationToken)
		{
			// ReSharper disable once SuspiciousTypeConversion.Global

			if (source is ReusableQueryable<T> queryable)
			{
				return queryable.ToReadOnlyCollection();
			}

			// ReSharper disable once SuspiciousTypeConversion.Global

			if (source is ReadOnlyCollection<T> readonlyCollection)
			{
				return readonlyCollection;
			}

			// ReSharper disable once SuspiciousTypeConversion.Global

			if (source is IList<T> list)
			{
				return new ReadOnlyCollection<T>(list);
			}

			// ReSharper disable once SuspiciousTypeConversion.Global

			var retval = !(source is ICollection<T> collection) ? new List<T>() : new List<T>(collection.Count);

			using (var enumerator = source.GetAsyncEnumeratorOrAdapt())
			{
				while (await enumerator.MoveNextAsync(cancellationToken))
				{
					cancellationToken.ThrowIfCancellationRequested();

					retval.Add(enumerator.Current);
				}
			}

			return new ReadOnlyCollection<T>(retval);
		}

		public static Task WithEachAsync<T>(this IEnumerable<T> source, Func<T, Task> value)
		{
			return source.WithEachAsync(value, CancellationToken.None);
		}

		public static Task WithEachAsync<T>(this IEnumerable<T> source, Func<T, Task<bool>> value)
		{
			return source.WithEachAsync(value, CancellationToken.None);
		}

		public static async Task WithEachAsync<T>(this IEnumerable<T> source, Func<T, Task<bool>> value, CancellationToken cancellationToken)
		{
			using (var enumerator = source.GetAsyncEnumeratorOrAdapt())
			{
				while (await enumerator.MoveNextAsync(cancellationToken))
				{
					cancellationToken.ThrowIfCancellationRequested();

					if (!await value(enumerator.Current).ConfigureAwait(false))
					{
						break;
					}
				}
			}
		}

		public static async Task WithEachAsync<T>(this IEnumerable<T> source, Func<T, Task> value, CancellationToken cancellationToken)
		{
			using (var enumerator = source.GetAsyncEnumeratorOrAdapt())
			{
				while (await enumerator.MoveNextAsync(cancellationToken))
				{
					await value(enumerator.Current).ConfigureAwait(false);

					cancellationToken.ThrowIfCancellationRequested();
				}
			}
		}

		public static Task WithEachAsync<T>(this IAsyncEnumerable<T> source, Func<T, Task> value)
		{
			return source.WithEachAsync(value, CancellationToken.None);
		}

		public static Task WithEachAsync<T>(this IAsyncEnumerable<T> source, Func<T, Task<bool>> value)
		{
			return source.WithEachAsync(value, CancellationToken.None);
		}

		public static Task WithEachAsync<T>(this IAsyncEnumerable<T> source, Func<T, Task<bool>> value, CancellationToken cancellationToken)
		{
			return ((IEnumerable<T>)source).WithEachAsync(value, cancellationToken);
		}

		public static Task WithEachAsync<T>(this IAsyncEnumerable<T> source, Func<T, Task> value, CancellationToken cancellationToken)
		{
			return ((IEnumerable<T>)source).WithEachAsync(value, cancellationToken);
		}
	}
}
