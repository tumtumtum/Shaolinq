using System.Collections.Generic;

namespace Shaolinq
{
	public static class DictionaryExtensions
	{
		public static Dictionary<K, V> Clone<K, V>(this Dictionary<K, V> self, K key, V value)
		{
			return new Dictionary<K, V>(self, self.Comparer) { [key] = value };
		}
	}
}
