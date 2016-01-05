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
		private readonly string outerAlias;
		private readonly List<SqlColumnExpression> replacedColumns;

		private SqlOuterQueryReferencePlaceholderSubstitutor(int placeholderCount, string outerAlias, List<SqlColumnExpression> replacedColumns)
		{
			this.placeholderCount = placeholderCount;
			this.outerAlias = outerAlias;
			this.replacedColumns = replacedColumns;
		}

		public static Expression Substitute(Expression expression, string outerAlias, ref int placeholderCount, List<SqlColumnExpression> replacedColumns)
		{
			var visitor = new SqlOuterQueryReferencePlaceholderSubstitutor(placeholderCount, outerAlias, replacedColumns);
			var retval = visitor.Visit(expression);

			placeholderCount = visitor.placeholderCount;
			
			return retval;
		}

		protected override Expression VisitColumn(SqlColumnExpression columnExpression)
		{
			if (columnExpression.SelectAlias == outerAlias)
			{
				replacedColumns.Add(columnExpression);

                return new SqlConstantPlaceholderExpression(this.placeholderCount++, Expression.Constant(columnExpression.Type.GetDefaultValue(), columnExpression.Type));
			}

			return base.VisitColumn(columnExpression);
		}
	}
}
