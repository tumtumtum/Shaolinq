// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System;

namespace Shaolinq.Persistence.Linq.Expressions
{
	public class SqlAliasedExpression
		: SqlBaseExpression
	{
		public string Alias { get; }

		public SqlAliasedExpression(Type type, string alias)
			: base(type)
		{
			this.Alias = alias;
		}
	}
}