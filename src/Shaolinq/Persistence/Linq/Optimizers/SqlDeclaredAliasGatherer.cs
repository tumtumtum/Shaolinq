// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System.Collections.Generic;
using System.Linq.Expressions;
using Shaolinq.Persistence.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Optimizers
{
	public class SqlDeclaredAliasGatherer : SqlExpressionVisitor
	{
		private readonly HashSet<string> aliases = new HashSet<string>();

		private SqlDeclaredAliasGatherer()
		{
		}

		public static HashSet<string> Gather(Expression source)
		{
			var gatherer = new SqlDeclaredAliasGatherer();

			gatherer.Visit(source);

			return gatherer.aliases;
		}

		protected override Expression VisitSelect(SqlSelectExpression select)
		{
			this.aliases.Add(select.Alias);

			return select;
		}

		protected override Expression VisitTable(SqlTableExpression table)
		{
			this.aliases.Add(table.Alias);

			return table;
		}
	}
}
