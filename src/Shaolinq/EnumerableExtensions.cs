// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Shaolinq.Persistence;

namespace Shaolinq
{
    public static partial class EnumerableExtensions
	{
        public static IAsyncEnumerable<T> DefaultIfEmptyAsync<T>(this IEnumerable<T> enumerable)
        {
            return enumerable.DefaultIfEmptyAsync(default(T));
        }

        public static IAsyncEnumerable<T> DefaultIfEmptyAsync<T>(this IEnumerable<T> enumerable, T defaultValue)
        {
            return new AsyncEnumerableAdapter<T>(() => new DefaultIfEmptyEnumerator<T>(enumerable.GetAsyncEnumerator(), defaultValue));
        }

        public static IEnumerable<T?> DefaultIfEmptyCoalesceSpecifiedValue<T>(this IEnumerable<T?> enumerable, T? specifiedValue)
			where T : struct
        {
            return enumerable.DefaultIfEmptyCoalesceSpecifiedValueAsync(specifiedValue);
        }

	    public static IAsyncEnumerable<T?> DefaultIfEmptyCoalesceSpecifiedValueAsync<T>(this IEnumerable<T?> enumerable, T? specifiedValue)
            where T : struct
        {
            return new AsyncEnumerableAdapter<T?>(() => new DefaultIfEmptyCoalesceSpecifiedValueEnumerator<T>(enumerable.GetAsyncEnumerator(), specifiedValue));
        }
		
		public static IEnumerable<T> EmptyIfFirstIsNull<T>(this IEnumerable<T> enumerable)
		{
            return new AsyncEnumerableAdapter<T>(() => new EmptyIfFirstIsNullEnumerator<T>(enumerable.GetAsyncEnumerator()));
        }

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static IAsyncEnumerable<T> EmptyIfFirstIsNullAsync<T>(this IEnumerable<T> enumerable, CancellationToken cancellationToken)
		{
			return new AsyncEnumerableAdapter<T>(() => new EmptyIfFirstIsNullEnumerator<T>(enumerable.GetAsyncEnumerator()));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static IEnumerator<T> GetEnumeratorEx<T>(this IEnumerable<T> enumerable) => enumerable.GetEnumerator();

		internal static Task<IAsyncEnumerator<T>> GetEnumeratorExAsync<T>(this IEnumerable<T> enumerable)
		{
			return Task.FromResult(enumerable.GetAsyncEnumerator());
		}

		internal static IAsyncEnumerator<T> GetAsyncEnumerator<T>(this IEnumerable<T> enumerable)
		{
			var asyncEnumerable = enumerable as IAsyncEnumerable<T>;

			if (asyncEnumerable == null)
			{
				return new AsyncEnumeratorAdapter<T>(enumerable.GetEnumerator());
			}

			return asyncEnumerable.GetAsyncEnumerator();
		}

		internal static IAsyncEnumerator<T> GetAsyncEnumeratorOrThrow<T>(this IEnumerable<T> enumerable)
		{
			var asyncEnumerable = enumerable as IAsyncEnumerable<T>;

			if (asyncEnumerable == null)
			{
				throw new NotSupportedException();
			}

			return asyncEnumerable.GetAsyncEnumerator();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static bool MoveNextEx<T>(this IEnumerator<T> enumerator)
	    {
	        return enumerator.MoveNext();
	    }

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static Task<bool> MoveNextExAsync<T>(this IAsyncEnumerator<T> enumerator, CancellationToken cancellationToken)
        {
            return enumerator.MoveNextAsync(cancellationToken);
        }

        [RewriteAsync(true)]
        private static int Count<T>(this IEnumerable<T> enumerable)
        {
            var list = enumerable as IList<T>;

            if (list != null)
            {
                return list.Count;
            }

            var retval = 0;

            using (var enumerator = enumerable.GetEnumeratorEx())
            {
                retval++;
            }

            return retval;
        }

        [RewriteAsync(true)]
        private static long LongCount<T>(this IEnumerable<T> enumerable)
        {
            var list = enumerable as IList<T>;

            if (list != null)
            {
                return list.Count;
            }

            var retval = 0L;

            using (var enumerator = enumerable.GetEnumeratorEx())
            {
                retval++;
            }

            return retval;
        }

        [RewriteAsync(true)]
        public static T SingleOrSpecifiedValueIfFirstIsDefaultValue<T>(this IEnumerable<T> enumerable, T specifiedValue)
		{
            using (var enumerator = enumerable.GetEnumeratorEx())
            {
                if (!enumerator.MoveNextEx())
                {
                    return new T[0].Single();
                }

                var result = enumerator.Current;

                if (enumerator.MoveNextEx())
                {
                    return new T[2].Single();
                }

                if (object.Equals(result, default(T)))
                {
                    return specifiedValue;
                }

                return result;
            }
        }

        [RewriteAsync(true)]
        private static T Single<T>(this IEnumerable<T> enumerable)
	    {
	        using (var enumerator = enumerable.GetEnumeratorEx())
	        {
	            if (!enumerator.MoveNextEx())
	            {
	               return new T[0].Single();
	            }

	            var result = enumerator.Current;

                if (enumerator.MoveNextEx())
                {
                    return new T[2].Single();
                }

	            return result;
	        }
	    }

        [RewriteAsync(true)]
        private static T SingleOrDefault<T>(this IEnumerable<T> enumerable)
        {
            using (var enumerator = enumerable.GetEnumeratorEx())
            {
                if (!enumerator.MoveNextEx())
                {
                    return default(T);
                }

                var result = enumerator.Current;

                if (enumerator.MoveNextEx())
                {
                    return new T[2].Single();
                }

                return result;
            }
        }

        [RewriteAsync(true)]
	    private static T First<T>(this IEnumerable<T> enumerable)
	    {
            using (var enumerator = enumerable.GetEnumeratorEx())
            {
                if (!enumerator.MoveNextEx())
                {
                    return Enumerable.Empty<T>().First();
                }

                return enumerator.Current;
            }
        }

        [RewriteAsync(true)]
        private static T FirstOrDefault<T>(this IEnumerable<T> enumerable)
        {
            using (var enumerator = enumerable.GetEnumeratorEx())
            {
                if (!enumerator.MoveNextEx())
                {
                    return Enumerable.Empty<T>().First();
                }

                return enumerator.Current;
            }
        }

        [RewriteAsync(true)]
        public static T SingleOrExceptionIfFirstIsNull<T>(this IEnumerable<T?> enumerable)
			where T : struct
		{
			using (var enumerator = enumerable.GetEnumeratorEx())
			{
				if (!enumerator.MoveNextEx() || enumerator.Current == null)
				{
					throw new InvalidOperationException("Sequence contains no elements");
				}

				return enumerator.Current.Value;
			}
		}
        
        [RewriteAsync(true)]
        public static void WithEach<T>(this IEnumerable<T> enumerable, Action<T> value)
        {
            using (var enumerator = enumerable.GetEnumeratorEx())
            {
                while (enumerator.MoveNextEx())
                {
                    value(enumerator.Current);
                }
            }
        }

        [RewriteAsync(true)]
        public static void WithEach<T>(this IEnumerable<T> enumerable, Func<T, bool> value)
        {
            using (var enumerator = enumerable.GetEnumeratorEx())
            {
                while (enumerator.MoveNextEx())
                {
                    if (!value(enumerator.Current))
                    {
                        break;
                    }
                }
            }
        }

        public static Task WithEachAsync<T>(this IEnumerable<T> enumerable, Func<T, Task> value)
        {
            return enumerable.EachAsync(value, CancellationToken.None);
        }

        public static async Task EachAsync<T>(this IEnumerable<T> enumerable, Func<T, Task> value, CancellationToken cancellationToken)
        {
            using (var enumerator = await enumerable.GetEnumeratorExAsync())
            {
                while (await enumerator.MoveNextAsync(cancellationToken))
                {
                    await value(enumerator.Current).ConfigureAwait(false);

                    if (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }
                }
            }
        }

        [RewriteAsync(true)]
        private static List<T> ToList<T>(this IEnumerable<T> enumerable)
        {
            if (enumerable == null)
            {
                return null;
            }

            var result = enumerable as List<T>;

            if (result != null)
            {
                return result;
            }

            result = new List<T>();

            using (var enumerator = enumerable.GetEnumeratorEx())
            {
                while (enumerator.MoveNextEx())
                {
                    result.Add(enumerator.Current);
                }
            }

            return result;
        }

        [RewriteAsync(true)]
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
