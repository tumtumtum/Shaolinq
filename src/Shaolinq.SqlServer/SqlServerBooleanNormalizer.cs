// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Platform;
using Shaolinq.Persistence.Linq;
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
		    if ((binaryExpression.NodeType == ExpressionType.Or
		         || binaryExpression.NodeType == ExpressionType.And
		         || binaryExpression.NodeType == ExpressionType.OrElse
		         || binaryExpression.NodeType == ExpressionType.AndAlso)
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
		            right = Expression.Equal(right, Expression.Constant(true));
		        }

		        if (left != binaryExpression.Left || right != binaryExpression.Right)
		        {
		            return Expression.MakeBinary(binaryExpression.NodeType, left, right);
		        }

                var expression = this.Visit(binaryExpression.Conversion);

		        if (left == binaryExpression.Left && right == binaryExpression.Right && expression == binaryExpression.Conversion)
		        {
		            return binaryExpression;
		        }

		        if (binaryExpression.NodeType == ExpressionType.Coalesce && binaryExpression.Conversion != null)
		        {
		            return Expression.Coalesce(left, right, expression as LambdaExpression);
		        }

		        return Expression.MakeBinary(binaryExpression.NodeType, left, right, binaryExpression.IsLiftedToNull, binaryExpression.Method);
		    }
		    else
		    {
		        return base.VisitBinary(binaryExpression);
		    }
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

		protected override Expression VisitJoin(SqlJoinExpression join)
		{
			var left = this.Visit(join.Left);
			var right = this.Visit(join.Right);
			var condition = this.Visit(join.JoinCondition);

			if (condition?.Type.GetUnwrappedNullableType() == typeof(bool) && condition is BitBooleanExpression)
			{
				condition = Expression.Equal(condition, Expression.Constant(true));
			}

			if (left != join.Left || right != join.Right || condition != join.JoinCondition)
			{
				return new SqlJoinExpression(join.Type, join.JoinType, left, right, condition);
			}

			return join;
		}

		protected override Expression VisitSelect(SqlSelectExpression selectExpression)
		{
			var count = selectExpression.Columns.Count;
			List<SqlColumnDeclaration> newColumns = null;

			for (var i = 0; i < count; i++)
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

			var from = this.VisitSource(selectExpression.From);
			var where = this.Visit(selectExpression.Where);
			var orderBy = this.VisitExpressionList(selectExpression.OrderBy);
			var groupBy = this.VisitExpressionList(selectExpression.GroupBy);
			var skip = this.Visit(selectExpression.Skip);
			var take = this.Visit(selectExpression.Take);
			
			if (where is BitBooleanExpression)
			{
				where = Expression.Equal(where, Expression.Constant(true));
			}

			if (from != selectExpression.From || where != selectExpression.Where || newColumns != selectExpression.Columns || orderBy != selectExpression.OrderBy || groupBy != selectExpression.GroupBy || take != selectExpression.Take || skip != selectExpression.Skip)
			{
				return new SqlSelectExpression(selectExpression.Type, selectExpression.Alias, newColumns ?? selectExpression.Columns, from, where, orderBy, groupBy, selectExpression.Distinct, skip, take, selectExpression.ForUpdate);
			}

			return base.VisitSelect(selectExpression);
		}
	}
}
