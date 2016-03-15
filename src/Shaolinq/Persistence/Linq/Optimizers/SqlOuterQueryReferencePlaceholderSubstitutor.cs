// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Platform;
using Shaolinq.Persistence.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Optimizers
{
	public class SqlOuterQueryReferencePlaceholderSubstitutor
		: SqlExpressionVisitor
	{
		private int placeholderCount;
		private readonly HashSet<string> aliases;
		private readonly List<Expression> replacedExpressions;

		private SqlOuterQueryReferencePlaceholderSubstitutor(int placeholderCount, HashSet<string> aliases, List<Expression> replacedExpressions)
		{
			this.placeholderCount = placeholderCount;
			this.aliases = aliases;
			this.replacedExpressions = replacedExpressions;
		}

		public static Expression Substitute(Expression expression, ref int placeholderCount, List<Expression> replacedExpressions)
		{
			var aliases = SqlDeclaredAliasesGatherer.Gather(expression);
			var visitor = new SqlOuterQueryReferencePlaceholderSubstitutor(placeholderCount, aliases, replacedExpressions);

			var retval = visitor.Visit(expression);

			placeholderCount = visitor.placeholderCount;
			
			return retval;
		}

		protected override Expression VisitConstantPlaceholder(SqlConstantPlaceholderExpression constantPlaceholder)
		{
			var nullableType = constantPlaceholder.Type.MakeNullable();

			if (nullableType == constantPlaceholder.Type)
			{
				replacedExpressions.Add(constantPlaceholder);

				return new SqlConstantPlaceholderExpression(this.placeholderCount++, Expression.Constant(null, nullableType));
			}
			else
			{
				replacedExpressions.Add(new SqlConstantPlaceholderExpression(constantPlaceholder.Index, Expression.Constant(constantPlaceholder.ConstantExpression.Value, nullableType)));

				return new SqlConstantPlaceholderExpression(this.placeholderCount++, Expression.Constant(null, nullableType));
			}
		}

		protected override Expression VisitColumn(SqlColumnExpression columnExpression)
		{
			if (!aliases.Contains(columnExpression.SelectAlias))
			{
				var nullableType = columnExpression.Type.MakeNullable();

				if (nullableType == columnExpression.Type)
				{
					replacedExpressions.Add(columnExpression);

					return new SqlConstantPlaceholderExpression(this.placeholderCount++, Expression.Constant(null, columnExpression.Type.MakeNullable()));
				}
				else
				{
					replacedExpressions.Add(columnExpression.ChangeToNullable());

					return Expression.Convert(new SqlConstantPlaceholderExpression(this.placeholderCount++, Expression.Constant(null, columnExpression.Type.MakeNullable())), columnExpression.Type);
				}
			}

			return base.VisitColumn(columnExpression);
		}
	}
}
