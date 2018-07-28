// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)

using System.Linq.Expressions;
using Shaolinq.Persistence.Linq;
using Shaolinq.Persistence.Linq.Expressions;

namespace Shaolinq.MySql
{
	public class MySqlIndexConditionRemover
		: SqlExpressionVisitor
	{
		public static Expression Remove(Expression expression)
		{
			return new MySqlIndexConditionRemover().Visit(expression);
		}

		protected override Expression VisitCreateIndex(SqlCreateIndexExpression createIndexExpression)
		{
			return base.VisitCreateIndex(createIndexExpression.ChangeWhere(null));
		}
	}
}