// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Linq.Expressions;
using Shaolinq.Persistence.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Optimizers
{
	public class SqlInsertIntoNormalizer
		: SqlExpressionVisitor
	{
		public static Expression Normalize(Expression expression)
		{
			return new SqlInsertIntoNormalizer().Visit(expression);
		}

		protected override Expression VisitInsertInto(SqlInsertIntoExpression insertIntoExpression)
		{
			var projection = insertIntoExpression.Source as SqlProjectionExpression;

			if (projection == null)
			{
				return insertIntoExpression;
			}

			if (projection.Select.From.NodeType != (ExpressionType)SqlExpressionType.Table)
			{
				throw new NotSupportedException();
			}

			var table = (SqlTableExpression)projection.Select.From;
			var alias = table.Alias;
			var where = AliasReferenceReplacer.Replace(projection.Select.Where, alias, table.Name);

			if (where != null)
			{
				throw new InvalidOperationException("Inserts must only be performed on pure tables");
			}

			return new SqlInsertIntoExpression(table, insertIntoExpression.ColumnNames, insertIntoExpression.ReturningAutoIncrementColumnNames, insertIntoExpression.ValueExpressions);
		}
	}
}