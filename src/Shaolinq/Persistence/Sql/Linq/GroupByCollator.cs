// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Shaolinq.Persistence.Sql.Linq.Expressions;

namespace Shaolinq.Persistence.Sql.Linq
{
	public class GroupByCollator
		: SqlExpressionVisitor
	{
		private GroupByCollator()
		{
		}

		public static Expression Collate(Expression expression)
		{
			var visitor = new GroupByCollator();

			return visitor.Visit(expression);
		}

		protected override Expression VisitSelect(SqlSelectExpression selectExpression)
		{
			if (selectExpression.GroupBy != null && selectExpression.GroupBy.Count == 1
				&& selectExpression.GroupBy[0].NodeType == ExpressionType.New)
			{
				var groupBy = ((NewExpression)selectExpression.GroupBy[0]).Arguments;

				return new SqlSelectExpression(selectExpression.Type, selectExpression.Alias, selectExpression.Columns, selectExpression.From, selectExpression.Where, selectExpression.OrderBy, groupBy, selectExpression.Distinct, selectExpression.Skip, selectExpression.Take, selectExpression.ForUpdate);	
			}

			return base.VisitSelect(selectExpression);
		}
	}
}
