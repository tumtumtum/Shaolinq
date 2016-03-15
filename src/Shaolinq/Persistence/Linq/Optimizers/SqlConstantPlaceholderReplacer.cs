// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

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
				var retval = Expression.ArrayIndex(this.placeholderValues, Expression.Constant(constantPlaceholder.Index));

				return retval.Type == constantPlaceholder.Type ? retval : (Expression)Expression.Convert(retval, constantPlaceholder.Type);
			}
			else
			{
				if (constantPlaceholder.Index < this.placeholderValuesLiteral.Length)
				{
					var retval = Expression.Constant(this.placeholderValuesLiteral[constantPlaceholder.Index]);

					return retval.Type == constantPlaceholder.Type ? retval : (Expression)Expression.Convert(retval, constantPlaceholder.Type);
				}
				else
				{
					return constantPlaceholder;
				}
			}
		}
	}
}