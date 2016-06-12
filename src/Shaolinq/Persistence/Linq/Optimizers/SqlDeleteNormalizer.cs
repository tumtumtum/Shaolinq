// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Linq.Expressions;
using Shaolinq.Persistence.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Optimizers
{
	public class SqlDeleteNormalizer
		: SqlExpressionVisitor
	{
		public static Expression Normalize(Expression expression)
		{
			return new SqlDeleteNormalizer().Visit(expression);
		}
		
		protected override Expression VisitDelete(SqlDeleteExpression deleteExpression)
		{
			var projection = deleteExpression.Source as SqlProjectionExpression;

			if (projection == null)
			{
				return deleteExpression;
			}
			
			if (projection.Select.From.NodeType != (ExpressionType)SqlExpressionType.Table)
			{
				throw new NotSupportedException();
			}

			var table = (SqlTableExpression)projection.Select.From;
			var alias = table.Alias;
			var where = AliasReferenceReplacer.Replace(projection.Select.Where, alias, table.Name);
			
			return new SqlDeleteExpression(table, where);
		}
	}
}
