using System;
using System.Collections.Generic;

namespace Shaolinq
{
	internal struct CompositePrimaryKey
	{
		internal readonly ObjectPropertyValue[] keyValues;

		public CompositePrimaryKey(ObjectPropertyValue[] keyValues)
		{
			this.keyValues = keyValues;
		}
	}

	internal class CompositePrimaryKeyComparer
		: IEqualityComparer<CompositePrimaryKey>
	{
		public static readonly CompositePrimaryKeyComparer Default = new CompositePrimaryKeyComparer();
			
		public bool Equals(CompositePrimaryKey x, CompositePrimaryKey y)
		{
			if (x.keyValues.Length != y.keyValues.Length)
			{
				return false;
			}

			for (int i = 0, n = x.keyValues.Length; i < n; i++)
			{
				if (!Equals(x.keyValues[i], y.keyValues[i]))
				{
					return false;
				}
			}

			return true;
		}

		public int GetHashCode(CompositePrimaryKey obj)
		{
			var retval = obj.keyValues.Length;

			for (int i = 0, n = Math.Min(retval, 8); i < n;  i++)
			{
				retval ^= obj.keyValues[i].GetHashCode();
			}

			return retval;
		}
	}
}