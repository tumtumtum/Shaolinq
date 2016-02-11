// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System.Linq.Expressions;
using Shaolinq.Persistence.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Optimizers
{
	public class SqlConstantPlaceholderReplacer
		: SqlExpressionVisitor
	{
		private readonly Expression placeholderValues;
		private readonly object[] placeholderValuesLiteral;

		private SqlConstantPlaceholderReplacer(Expression placeholderValues)
		{
			this.placeholderValues = placeholderValues;
		}

		private SqlConstantPlaceholderReplacer(object[] placeholderValuesLiteral)
		{
			this.placeholderValuesLiteral = placeholderValuesLiteral;
		}

		public static Expression Replace(Expression expression, Expression placeholderValues)
		{
			return new SqlConstantPlaceholderReplacer(placeholderValues).Visit(expression);
		}

		public static Expression Replace(Expression expression, object[] placeholderValues)
		{
			return new SqlConstantPlaceholderReplacer(placeholderValues).Visit(expression);
		}

		protected override Expression VisitConstantPlaceholder(SqlConstantPlaceholderExpression constantPlaceholder)
		{
			if (placeholderValues != null)
			{
				return Expression.Convert(Expression.ArrayIndex(this.placeholderValues, Expression.Constant(constantPlaceholder.Index)), constantPlaceholder.Type);
			}
			else
			{
				if (constantPlaceholder.Index < this.placeholderValuesLiteral.Length)
				{
					return Expression.Convert(Expression.Constant(this.placeholderValuesLiteral[constantPlaceholder.Index]), constantPlaceholder.Type);
				}
				else
				{
					return constantPlaceholder;
				}
			}
		}
	}
}