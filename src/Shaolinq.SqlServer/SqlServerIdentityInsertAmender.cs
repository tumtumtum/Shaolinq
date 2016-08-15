// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System.Collections.Generic;
using System.Linq.Expressions;
using Shaolinq.Persistence.Linq;
using Shaolinq.Persistence.Linq.Expressions;

namespace Shaolinq.SqlServer
{
	public class SqlServerIdentityInsertAmender
		: SqlExpressionVisitor
	{
		public static Expression Amend(Expression expression)
		{
			return new SqlServerIdentityInsertAmender().Visit(expression);
		}

		protected override Expression VisitInsertInto(SqlInsertIntoExpression expression)
		{
			if (!expression.RequiresIdentityInsert)
			{
				return base.VisitInsertInto(expression);
			}

			var list = new List<Expression>
			{
				new SqlSetCommandExpression("IDENTITY_INSERT", expression.Source, new SqlKeywordExpression("ON")),
				base.VisitInsertInto(expression),
				new SqlSetCommandExpression("IDENTITY_INSERT", expression.Source, new SqlKeywordExpression("OFF")),
			};

			return new SqlStatementListExpression(list);
		}
	}
}