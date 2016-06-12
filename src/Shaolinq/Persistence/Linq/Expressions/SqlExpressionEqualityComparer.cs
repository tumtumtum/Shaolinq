// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System.Collections.Generic;
using System.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Expressions
{
	public class SqlExpressionEqualityComparer
		: IEqualityComparer<Expression>
	{
		private readonly SqlExpressionComparerOptions options;
		public static readonly SqlExpressionEqualityComparer Default = new SqlExpressionEqualityComparer();

		public SqlExpressionEqualityComparer()
			: this(SqlExpressionComparerOptions.None)
		{
		}

		public SqlExpressionEqualityComparer(SqlExpressionComparerOptions options)
		{
			this.options = options;
		}

		public bool Equals(Expression x, Expression y)
		{
			return SqlExpressionComparer.Equals(x, y, options);
		}

		public int GetHashCode(Expression obj)
		{
			return SqlExpressionHasher.Hash(obj, options);
		}
	}
}