// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

﻿using System;
using System.Linq.Expressions;
using Shaolinq.Persistence.Sql.Linq.Expressions;

namespace Shaolinq.Persistence.Sql.Linq.Optimizer
{
	/// <summary>
	/// Converts binary expressions between two <see cref="SqlObjectOperand"/> expressions
	/// into multiple binary expressions performing the operation over the the primary
	/// keys of the object operands.
	/// </summary>
	public class ObjectOperandComparisonExpander
		: SqlExpressionVisitor
	{
		private bool inProjector;

		private ObjectOperandComparisonExpander()
		{
		}

		public static Expression Expand(Expression expression)
		{
			var fixer = new ObjectOperandComparisonExpander();

			return fixer.Visit(expression);
		}

		protected override Expression VisitProjection(SqlProjectionExpression projection)
		{
			var source = (SqlSelectExpression)Visit(projection.Select);

			var oldInProjector = inProjector;

			inProjector = true;

			Expression projector;

			try
			{
				projector = Visit(projection.Projector);
			}
			finally
			{
				inProjector = oldInProjector;
			}

			var aggregator = (LambdaExpression)Visit(projection.Aggregator);

			if (source != projection.Select
				|| projector != projection.Projector
				|| aggregator != projection.Aggregator)
			{
				return new SqlProjectionExpression(source, projector, aggregator, projection.IsElementTableProjection, projection.SelectFirstType, null);
			}

			return projection;
		}

		protected override Expression VisitFunctionCall(SqlFunctionCallExpression functionCallExpression)
		{
			if (this.inProjector)
			{
				return functionCallExpression;
			}

			if (functionCallExpression.Arguments.Count == 1)
			{
				if (functionCallExpression.Arguments[0] is SqlObjectOperand)
				{
					Expression retval = null;
					var operand = (SqlObjectOperand)functionCallExpression.Arguments[0];

					for (int i = 0, count = operand.ExpressionsInOrder.Count; i < count; i++)
					{
						var left = operand.ExpressionsInOrder[i];

						var current = new SqlFunctionCallExpression(functionCallExpression.Type, functionCallExpression.Function, left);

						if (retval == null)
						{
							retval = current;
						}
						else
						{
							retval = Expression.And(retval, current);
						}
					}

					return retval;
				}
			}

			return base.VisitFunctionCall(functionCallExpression);
		}

		protected override Expression VisitBinary(BinaryExpression binaryExpression)
		{
			if (inProjector)
			{
				return binaryExpression;
			}

			if (binaryExpression.Left.NodeType == (ExpressionType)SqlExpressionType.ObjectOperand
				&& binaryExpression.Right.NodeType == (ExpressionType)SqlExpressionType.ObjectOperand)
			{
				Expression retval = null;
				var leftOperand = (SqlObjectOperand)binaryExpression.Left;
				var rightOperand = (SqlObjectOperand)binaryExpression.Right;

				for (int i = 0, count = leftOperand.ExpressionsInOrder.Count; i < count; i++)
				{
					Expression current;
					var left = leftOperand.ExpressionsInOrder[i];
					var right = rightOperand.ExpressionsInOrder[i];
					
					switch (binaryExpression.NodeType)
					{
						case ExpressionType.Equal:
							current = Expression.Equal(left, right);
							break;
						case ExpressionType.NotEqual:
							current = Expression.NotEqual(left, right);
							break;
						default:
							throw new NotSupportedException(String.Format("Operation on DataAccessObject with {0} not supported", binaryExpression.NodeType.ToString()));
					}
					
					if (retval == null)
					{
						retval = current;
					}
					else
					{
						retval = Expression.And(retval, current);
					}
				}

				return retval;
			}

			return base.VisitBinary(binaryExpression);
		}
	}
}
