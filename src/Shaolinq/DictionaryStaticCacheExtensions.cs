using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Shaolinq.Logging;

namespace Shaolinq
{
	public static class DictionaryStaticCacheExtensions
	{
		private static readonly ILog staticCacheLogger = LogProvider.GetLogger("Shaolinq.StaticCacheLogger");

		public static Dictionary<K, V> Clone<K, V>(this Dictionary<K, V> self, K key, V value, string cacheName = "", int limit = 1024, ILog logger = null, Func<V, string> valueToString = null)
		{
			if (self.Count >= limit)
			{
				(logger ?? staticCacheLogger).Debug(() => $"Sealing {cacheName ?? "CacheName"} because it overflowed with a size of {limit}\n\nValue: {valueToString?.Invoke(value) ?? key.ToString()}\n\nAt: {new StackTrace()}");

				return self;
			}

			return new Dictionary<K, V>(self) { [key] = value };
		}
	}
}
