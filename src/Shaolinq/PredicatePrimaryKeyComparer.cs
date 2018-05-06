using System.Collections.Generic;
using System.Linq.Expressions;
using Shaolinq.Persistence.Linq.Expressions;

namespace Shaolinq
{
	internal struct PredicatePrimaryKey
	{
		internal readonly LambdaExpression predicate;

		public PredicatePrimaryKey(LambdaExpression predicate)
		{
			this.predicate = predicate;
		}
	}

	internal class PredicatePrimaryKeyComparer
		: IEqualityComparer<PredicatePrimaryKey>
	{
		public static readonly PredicatePrimaryKeyComparer Default = new PredicatePrimaryKeyComparer();

		public bool Equals(PredicatePrimaryKey x, PredicatePrimaryKey y)
		{
			return SqlExpressionComparer.Equals(x.predicate, y.predicate);
		}

		public int GetHashCode(PredicatePrimaryKey obj)
		{
			return SqlExpressionHasher.Hash(obj.predicate);
		}
	}
}