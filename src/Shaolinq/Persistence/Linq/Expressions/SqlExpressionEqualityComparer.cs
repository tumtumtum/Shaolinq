using System.Collections.Generic;
using System.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Expressions
{
	public class SqlExpressionEqualityComparer
		: IEqualityComparer<Expression>
	{
		public static readonly SqlExpressionEqualityComparer Default = new SqlExpressionEqualityComparer();

		private SqlExpressionEqualityComparer()
		{
		}
		
		public bool Equals(Expression x, Expression y)
		{
			return SqlExpressionComparer.Equals(x, y);
		}

		public int GetHashCode(Expression obj)
		{
			return SqlExpressionHasher.Hash(obj);
		}
	}
}