// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Shaolinq.Logging;

namespace Shaolinq.Persistence
{
	internal static class DictionaryStaticCacheExtensions
	{
		private static readonly ILog staticCacheLogger = LogProvider.GetLogger("Shaolinq.StaticCacheLogger");

		public static Dictionary<K, V> Clone<K, V>(this Dictionary<K, V> self, K key, V value, string cacheName = "", int limit = 1024, ILog logger = null, Func<V, string> valueToString = null)
		{
			if (self.Count >= limit)
			{
				(logger ?? staticCacheLogger).Debug(() => $"Sealing {cacheName ?? "CacheName"} because it overflowed with a size of {limit}\n\nValue: {valueToString?.Invoke(value) ?? value.ToString()}\n\nAt: {new StackTrace()}");

				return self;
			}

			var retval = new Dictionary<K, V>(self, self.Comparer) { [key] = value };

			if (!retval.ContainsKey(key))
			{
				(logger ?? staticCacheLogger).Error(() => $"Cache {cacheName ?? "CacheName"} has key that will never match.\n\nKey: {key}\n\nValue: {valueToString?.Invoke(value) ?? value.ToString()}\n\nAt: {new StackTrace()}");
			}

			return retval;
		}
	}
}
