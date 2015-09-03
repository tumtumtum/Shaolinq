// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Platform;
using Platform.Collections;
using Shaolinq.Persistence.Linq.Expressions;

namespace Shaolinq.SqlServer
{
	public class SqlServerBooleanNormalizer
		: SqlExpressionVisitor
	{
		public static Expression Normalize(Expression expression)
		{
			return new SqlServerBooleanNormalizer().Visit(expression);
		}

		protected override Expression VisitConstantPlaceholder(SqlConstantPlaceholderExpression constantPlaceholder)
		{
			var result = base.VisitConstant(constantPlaceholder.ConstantExpression);

			var retval = new SqlConstantPlaceholderExpression(constantPlaceholder.Index, (ConstantExpression)result);

			if (retval.Type.GetUnwrappedNullableType() == typeof(bool))
			{
				return new BitBooleanExpression(retval);
			}

			return retval;
		}

		protected override Expression VisitConstant(ConstantExpression constantExpression)
		{
			if (constantExpression.Type.GetUnwrappedNullableType() == typeof(bool))
			{
				return new BitBooleanExpression(constantExpression);
			}

			return base.VisitConstant(constantExpression);
		}

		protected override Expression VisitColumn(SqlColumnExpression columnExpression)
		{
			if (columnExpression.Type.GetUnwrappedNullableType() == typeof(bool))
			{
				return new BitBooleanExpression(columnExpression);
			}

			return base.VisitColumn(columnExpression);
		}

		protected override Expression VisitUnary(UnaryExpression unaryExpression)
		{
			if (unaryExpression.NodeType == ExpressionType.Not && unaryExpression.Type.GetUnwrappedNullableType() == typeof(bool))
			{
				var operand = this.Visit(unaryExpression.Operand);

				if (operand is BitBooleanExpression)
				{
					return Expression.MakeUnary(unaryExpression.NodeType, Expression.Equal(operand, Expression.Constant(true)), typeof(bool));
				}
			}

			return base.VisitUnary(unaryExpression);
		}

		protected override Expression VisitBinary(BinaryExpression binaryExpression)
		{
			if (binaryExpression.NodeType == ExpressionType.Or 
				|| binaryExpression.NodeType == ExpressionType.And
				|| binaryExpression.NodeType == ExpressionType.OrElse
				|| binaryExpression.NodeType == ExpressionType.AndAlso
				&& binaryExpression.Type.GetUnwrappedNullableType() == typeof(bool))
			{
				var left = this.Visit(binaryExpression.Left);
				var right = this.Visit(binaryExpression.Right);

				if (left.Type.GetUnwrappedNullableType() == typeof(bool) && (left is BitBooleanExpression))
				{
					left = Expression.Equal(left, Expression.Constant(true));
				}

				if (right.Type.GetUnwrappedNullableType() == typeof(bool) && (right is BitBooleanExpression))
				{
					right = Expression.Equal(right, Expression.Constant(false));
				}

				if (left != binaryExpression.Left || right != binaryExpression.Right)
				{
					return Expression.MakeBinary(binaryExpression.NodeType, left, right);
				}
			}

			return base.VisitBinary(binaryExpression);
		}

		protected override Expression VisitConditional(ConditionalExpression expression)
		{
			var test = this.Visit(expression.Test);
			var ifFalse = this.Visit(expression.IfFalse);
			var ifTrue = this.Visit(expression.IfTrue);

			if (test is BitBooleanExpression)
			{
				test = Expression.Equal(test, Expression.Constant(true));
			}

			if (ifFalse.Type.GetUnwrappedNullableType() == typeof(bool) && !(ifFalse is BitBooleanExpression))
			{
				ifFalse = BitBooleanExpression.Coerce(ifFalse);
			}

			if (ifTrue.Type.GetUnwrappedNullableType() == typeof(bool) && !(ifTrue is BitBooleanExpression))
			{
				ifTrue = BitBooleanExpression.Coerce(ifTrue);
			}

			if (test != expression.Test || ifFalse != expression.IfFalse || ifTrue != expression.IfTrue)
			{
				return new BitBooleanExpression(Expression.Condition(test, ifTrue, ifFalse));
			}
			else
			{
				return base.VisitConditional(expression);
			}
		}

		protected override Expression VisitSelect(SqlSelectExpression selectExpression)
		{
			List<SqlColumnDeclaration> newColumns = null;

			for (var i = 0; i < selectExpression.Columns.Count; i++)
			{
				var column = selectExpression.Columns[i];
				var visitedColumnExpression = this.Visit(column.Expression);

				if (visitedColumnExpression.Type.GetUnwrappedNullableType() == typeof(bool) && !(visitedColumnExpression is BitBooleanExpression))
				{
					if (newColumns == null)
					{
						newColumns = new List<SqlColumnDeclaration>(selectExpression.Columns.Take(i));
					}

					var newColumnExpression = BitBooleanExpression.Coerce(visitedColumnExpression);
					var newColumnDeclaration = new SqlColumnDeclaration(column.Name, newColumnExpression);

					newColumns.Add(newColumnDeclaration);
				}
				else if (visitedColumnExpression != column.Expression)
				{
					if (newColumns == null)
					{
						newColumns = new List<SqlColumnDeclaration>(selectExpression.Columns.Take(i));
					}

					newColumns.Add(column.ReplaceExpression(visitedColumnExpression));
				}
				else if (newColumns != null)
				{
					newColumns.Add(column);
				}
			}

			var where = this.Visit(selectExpression.Where);

			if ((where is BitBooleanExpression))
			{
				where = Expression.Equal(where, Expression.Constant(true));
			}

			if (where != selectExpression.Where)
			{
				if (newColumns != null)
				{
					return selectExpression.ChangeWhereAndColumns(where, new ReadOnlyList<SqlColumnDeclaration>(newColumns));
				}
				else
				{
					return selectExpression.ChangeWhere(where);
				}
			}
			else if (newColumns != null)
			{
				return selectExpression.ChangeColumns(newColumns, true);
			}
			else
			{
				return base.VisitSelect(selectExpression);
			}
		}
	}
}
