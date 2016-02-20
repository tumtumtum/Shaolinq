// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System.Collections.Generic;
using System.Linq;
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
		private readonly List<SqlColumnExpression> replacedColumns;

		private SqlOuterQueryReferencePlaceholderSubstitutor(int placeholderCount, HashSet<string> aliases, List<SqlColumnExpression> replacedColumns)
		{
			this.placeholderCount = placeholderCount;
			this.aliases = aliases;
			this.replacedColumns = replacedColumns;
		}

		public static Expression Substitute(Expression expression, ref int placeholderCount, List<SqlColumnExpression> replacedColumns)
		{
			var aliases = SqlDeclaredAliasesGatherer.Gather(expression);
			var visitor = new SqlOuterQueryReferencePlaceholderSubstitutor(placeholderCount, aliases, replacedColumns);

			var retval = visitor.Visit(expression);

			placeholderCount = visitor.placeholderCount;
			
			return retval;
		}

		protected override Expression VisitColumn(SqlColumnExpression columnExpression)
		{
			if (!aliases.Contains(columnExpression.SelectAlias))
			{
				var nullableType = columnExpression.Type.MakeNullable();

				if (nullableType == columnExpression.Type)
				{
					replacedColumns.Add(columnExpression);

					return new SqlConstantPlaceholderExpression(this.placeholderCount++, Expression.Constant(null, columnExpression.Type.MakeNullable()));
				}
				else
				{
					replacedColumns.Add(columnExpression.ChangeToNullable());

					return Expression.Convert(new SqlConstantPlaceholderExpression(this.placeholderCount++, Expression.Constant(null, columnExpression.Type.MakeNullable())), columnExpression.Type);
				}
			}

			return base.VisitColumn(columnExpression);
		}
	}
}
