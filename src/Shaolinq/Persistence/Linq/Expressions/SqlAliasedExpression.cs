// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System;

namespace Shaolinq.Persistence.Linq.Expressions
{
	public class SqlAliasedExpression
		: SqlBaseExpression, ISqlExposesAliases
	{
		public string Alias { get; }
		public string[] Aliases => new[] { this.Alias };

		public SqlAliasedExpression(Type type, string alias)
			: base(type)
		{
			this.Alias = alias;
		}
	}
}