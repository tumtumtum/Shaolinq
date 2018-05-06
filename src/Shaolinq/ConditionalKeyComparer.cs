using System.Collections.Generic;
using System.Linq.Expressions;
using Shaolinq.Persistence.Linq.Expressions;

namespace Shaolinq
{
	internal struct ConditionalKey
	{
		internal readonly Expression condition;

		public ConditionalKey(Expression condition)
		{
			this.condition = condition;
		}
	}

	internal class ConditionalKeyComparer
		: IEqualityComparer<ConditionalKey>
	{
		public static readonly ConditionalKeyComparer Default = new ConditionalKeyComparer();

		public bool Equals(ConditionalKey x, ConditionalKey y)
		{
			return SqlExpressionComparer.Equals(x.condition, y.condition, SqlExpressionComparerOptions.None);
		}

		public int GetHashCode(ConditionalKey obj)
		{
			return SqlExpressionHasher.Hash(obj.condition, SqlExpressionComparerOptions.None);
		}
	}
}