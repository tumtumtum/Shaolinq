// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System.Collections.Generic;
using System.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Expressions
{
	public class SqlExpressionEqualityComparer
	{
		public static readonly SqlExpressionEqualityComparer<Expression> Default = SqlExpressionEqualityComparer<Expression>.Default;
		public static readonly SqlExpressionEqualityComparer<Expression> IgnoreConstants = SqlExpressionEqualityComparer<Expression>.IgnoreConstants;
	}

	public class SqlExpressionEqualityComparer<T>
		: IEqualityComparer<T>
		where T : Expression
	{
		public static readonly SqlExpressionEqualityComparer<T> Default = new SqlExpressionEqualityComparer<T>();
		public static readonly SqlExpressionEqualityComparer<T> IgnoreConstants = new SqlExpressionEqualityComparer<T>(SqlExpressionComparerOptions.IgnoreConstants);

		private readonly SqlExpressionComparerOptions options;
		
		public SqlExpressionEqualityComparer()
			: this(SqlExpressionComparerOptions.None)
		{
		}

		public SqlExpressionEqualityComparer(SqlExpressionComparerOptions options)
		{
			this.options = options;
		}

		public bool Equals(T x, T y)
		{
			return SqlExpressionComparer.Equals(x, y, this.options);
		}

		public int GetHashCode(T obj)
		{
			return SqlExpressionHasher.Hash(obj, this.options);
		}
	}
}