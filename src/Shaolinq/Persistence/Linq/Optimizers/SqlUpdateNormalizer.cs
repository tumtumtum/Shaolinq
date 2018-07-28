// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)

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
			if (!(updateExpression.Source is SqlProjectionExpression projection))
			{
				return updateExpression;
			}

			if (projection.Select.From.NodeType != (ExpressionType)SqlExpressionType.Table)
			{
				throw new NotSupportedException();
			}

			var table = (SqlTableExpression)projection.Select.From;
			var alias = table.Alias;
			var where = SqlAliasReferenceReplacer.Replace(projection.Select.Where, alias, table.Name);

			return new SqlUpdateExpression(table, updateExpression.Assignments, where);
		}
	}
}