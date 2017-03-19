// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System.Collections.Generic;
using System.Linq.Expressions;
using Shaolinq.Persistence.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Optimizers
{
	public class SqlDeclaredAliasesGatherer : SqlExpressionVisitor
	{
		private readonly HashSet<string> aliases = new HashSet<string>();

		private SqlDeclaredAliasesGatherer()
		{
		}

		public static HashSet<string> Gather(Expression source)
		{
			var gatherer = new SqlDeclaredAliasesGatherer();

			gatherer.Visit(source);

			return gatherer.aliases;
		}

		protected override Expression VisitTable(SqlTableExpression table)
		{
			this.aliases.Add(table.Alias);

			return base.VisitTable(table);
		}

		protected override Expression VisitSelect(SqlSelectExpression selectExpression)
		{
			this.aliases.Add(selectExpression.Alias);

			return base.VisitSelect(selectExpression);
		}
	}
}