// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System.Linq.Expressions;
using Shaolinq.Persistence.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Optimizers
{
	public class SqlConstantPlaceholderReplacer
		: SqlExpressionVisitor
	{
		private readonly Expression placeholderValues;

		private SqlConstantPlaceholderReplacer(Expression placeholderValues)
		{
			this.placeholderValues = placeholderValues;
		}

		public static Expression Replace(Expression expression, Expression placeholderValues)
		{
			return new SqlConstantPlaceholderReplacer(placeholderValues).Visit(expression);
		}

		protected override Expression VisitConstantPlaceholder(SqlConstantPlaceholderExpression constantPlaceholder)
		{
			return Expression.Convert(Expression.ArrayIndex(this.placeholderValues, Expression.Constant(constantPlaceholder.Index)), constantPlaceholder.Type);
		}
	}
}