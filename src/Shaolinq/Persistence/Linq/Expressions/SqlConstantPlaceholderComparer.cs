using System.Collections.Generic;

namespace Shaolinq.Persistence.Linq.Expressions
{
	public class SqlConstantPlaceholderComparer
		: IEqualityComparer<SqlConstantPlaceholderExpression>
	{
		public static readonly SqlConstantPlaceholderComparer Default = new SqlConstantPlaceholderComparer();

		private SqlConstantPlaceholderComparer()
		{
		}

		public bool Equals(SqlConstantPlaceholderExpression x, SqlConstantPlaceholderExpression y)
		{
			return x.Index == y.Index && x.Type == y.Type;
		}

		public int GetHashCode(SqlConstantPlaceholderExpression obj)
		{
			return obj.Index;
		}
	}
}