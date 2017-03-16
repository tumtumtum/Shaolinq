// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

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

		protected override Expression VisitReferences(SqlReferencesExpression expression)
		{
			if (expression.Deferrability != SqlColumnReferenceDeferrability.NotDeferrable)
			{
				return expression.ChangeDeferrability(SqlColumnReferenceDeferrability.NotDeferrable);
			}

			return base.VisitReferences(expression);
		}

		public static Expression Remove(Expression expression)
		{
			return new SqlReferencesColumnDeferrabilityRemover().Visit(expression);
		}
	}
}
