// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Linq.Expressions;
using Shaolinq.Persistence.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Optimizers
{
	public class SqlAliasReferenceReplacer
		: SqlExpressionVisitor
	{
		private readonly string replacement;
		private readonly Func<string, bool> oldAliasMatch;

		private SqlAliasReferenceReplacer(Func<string, bool> oldAliasMatch, string replacement)
		{
			this.oldAliasMatch = oldAliasMatch;
			this.replacement = replacement;
		}

		public static Expression Replace(Expression expression, string oldAliasMatch, string replacement)
		{
			return new SqlAliasReferenceReplacer(c => c == oldAliasMatch, replacement).Visit(expression);
		}

		public static Expression Replace(Expression expression, Func<string, bool> oldAliasMatch, string replacement)
		{
			return new SqlAliasReferenceReplacer(oldAliasMatch, replacement).Visit(expression);
		}

		protected override Expression VisitColumn(SqlColumnExpression columnExpression)
		{
			if (this.oldAliasMatch(columnExpression.SelectAlias))
			{
				return columnExpression.ChangeAlias(this.replacement);
			}

			return base.VisitColumn(columnExpression);
		}
	}
}