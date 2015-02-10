// Copyright (c) 2007-2014 Thong Nguyen (tumtumtum@gmail.com)

using System.Linq.Expressions;
using Shaolinq.Persistence.Linq.Expressions;

namespace Shaolinq.Persistence.Linq
{
	public class SqlReferencesColumnDeferrabilityRemover
		: SqlExpressionVisitor
	{
		private SqlReferencesColumnDeferrabilityRemover()
		{	
		}

		protected override Expression VisitReferencesColumn(SqlReferencesColumnExpression referencesColumnExpression)
		{
			if (referencesColumnExpression.Deferrability != SqlColumnReferenceDeferrability.NotDeferrable)
			{
				return new SqlReferencesColumnExpression(referencesColumnExpression.ReferencedTable, SqlColumnReferenceDeferrability.NotDeferrable, referencesColumnExpression.ReferencedColumnNames, referencesColumnExpression.OnDeleteAction, referencesColumnExpression.OnUpdateAction);
			}

			return base.VisitReferencesColumn(referencesColumnExpression);
		}

		public static Expression Remove(Expression expression)
		{
			return new SqlReferencesColumnDeferrabilityRemover().Visit(expression);
		}
	}
}
