// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System.Linq.Expressions;
using Shaolinq.Persistence.Linq.Expressions;

namespace Shaolinq.SqlServer
{
	/// <summary>
	/// SQL Server requires sub-queries containing ORDER BY to use TOP (sigh)
	/// </summary>
	public class SqlServerSubqueryOrderByFixer
		: SqlExpressionVisitor
	{
		private bool isOuterMostSelect;

		private SqlServerSubqueryOrderByFixer()
		{
			this.isOuterMostSelect = true;
		}

		public static Expression Fix(Expression expression)
		{
			return new SqlServerSubqueryOrderByFixer().Visit(expression);
		}

		protected override Expression VisitSelect(SqlSelectExpression selectExpression)
		{
			var saveIsOuterMostSelect = this.isOuterMostSelect;

			try
			{
				this.isOuterMostSelect = false;

				if (!saveIsOuterMostSelect && selectExpression.OrderBy != null && selectExpression.Take == null)
				{
					return selectExpression.ChangeSkipTake(selectExpression.Skip, new SqlTakeAllValueExpression());
				}

				return base.VisitSelect(selectExpression);
			}
			finally
			{
				this.isOuterMostSelect = saveIsOuterMostSelect;
			}
		}
	}
}
