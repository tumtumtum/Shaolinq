// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Linq.Expressions;
using Shaolinq.Persistence.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Optimizers
{
	public class SqlUpdateNormalizer
		: SqlExpressionVisitor
	{
		public static Expression Normalize(Expression expression)
		{
			return new SqlUpdateNormalizer().Visit(expression);
		}

		protected override Expression VisitUpdate(SqlUpdateExpression updateExpression)
		{
			var projection = updateExpression.Source as SqlProjectionExpression;

			if (projection == null)
			{
				return updateExpression;
			}

			if (projection.Select.From.NodeType != (ExpressionType)SqlExpressionType.Table)
			{
				throw new NotSupportedException();
			}

			var table = (SqlTableExpression)projection.Select.From;
			var alias = table.Alias;
			var where = AliasReferenceReplacer.Replace(projection.Select.Where, alias, table.Name);

			return new SqlUpdateExpression(table, updateExpression.Assignments, where);
		}
	}
}