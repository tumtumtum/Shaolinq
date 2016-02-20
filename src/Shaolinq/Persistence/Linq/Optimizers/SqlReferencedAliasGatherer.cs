// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System.Collections.Generic;
using System.Linq.Expressions;
using Shaolinq.Persistence.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Optimizers
{
	public class SqlReferencedAliasGatherer : SqlExpressionVisitor
	{
		private readonly HashSet<string> aliases = new HashSet<string>();

		private SqlReferencedAliasGatherer()
		{
		}

		public static HashSet<string> Gather(Expression source)
		{
			var gatherer = new SqlReferencedAliasGatherer();

			gatherer.Visit(source);

			return gatherer.aliases;
		}

		protected override Expression VisitColumn(SqlColumnExpression column)
		{
			this.aliases.Add(column.SelectAlias);

			return column;
		}
	}
}
