// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Linq.Expressions;
using Shaolinq.Persistence.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Optimizers
{
	public class AliasReferenceReplacer
		: SqlExpressionVisitor
	{
		private readonly string replacement;
		private readonly Func<string, bool> oldAliasMatch;

		private AliasReferenceReplacer(Func<string, bool> oldAliasMatch, string replacement)
		{
			this.oldAliasMatch = oldAliasMatch;
			this.replacement = replacement;
		}

		public static Expression Replace(Expression expression, string oldAliasMatch, string replacement)
		{
			return new AliasReferenceReplacer(c => c == oldAliasMatch, replacement).Visit(expression);
		}

		public static Expression Replace(Expression expression, Func<string, bool> oldAliasMatch, string replacement)
		{
			return new AliasReferenceReplacer(oldAliasMatch, replacement).Visit(expression);
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