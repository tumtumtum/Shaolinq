// Copyright (c) 2007-2014 Thong Nguyen (tumtumtum@gmail.com)

﻿using System;
﻿using System.Collections.Generic;
﻿using System.Linq;
﻿using System.Linq.Expressions;
﻿using System.Reflection;
﻿using Shaolinq.Persistence.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Optimizers
{
	/// <summary>
	/// Converts binary expressions between two <see cref="SqlObjectReferenceExpression"/> expressions
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
			var expander = new ObjectOperandComparisonExpander();

			return expander.Visit(expression);
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
				return new SqlProjectionExpression(source, projector, aggregator, projection.IsElementTableProjection, projection.SelectFirstType, projection.DefaultValueExpression, projection.IsDefaultIfEmpty);
			}

			return projection;
		}

		private IEnumerable<Expression> GetPrimaryKeyElementalExpressions(Expression expression)
		{
			if (expression is MemberInitExpression)
			{
				var memberInitExpression = (MemberInitExpression)expression;

				foreach (var binding in memberInitExpression
					.Bindings
					.OfType<MemberAssignment>()
					.Where(c => c.Member is PropertyInfo).
					Where(binding => PropertyDescriptor.IsPropertyPrimaryKey((PropertyInfo)binding.Member)))
				{
					if (binding.Expression is MemberInitExpression || binding.Expression is SqlObjectReferenceExpression)
					{
						foreach (var value in GetPrimaryKeyElementalExpressions(binding.Expression))
						{
							yield return value;	
						}
					}
					else
					{
						yield return binding.Expression;
					}
				}
			}
			else if (expression is SqlObjectReferenceExpression)
			{
				var operand = expression as SqlObjectReferenceExpression;

				foreach (var value in operand
						.GetBindingsFlattened()
						.OfType<MemberAssignment>()
						.Select(c => c.Expression))
				{
					yield return value;
				}
			}
		}

		protected override Expression VisitFunctionCall(SqlFunctionCallExpression functionCallExpression)
		{
			if (this.inProjector)
			{
				return functionCallExpression;
			}

			if (functionCallExpression.Arguments.Count == 1
				&& (functionCallExpression.Function == SqlFunction.IsNotNull || functionCallExpression.Function == SqlFunction.IsNull)
				&& (functionCallExpression.Arguments[0].NodeType == ExpressionType.MemberInit || functionCallExpression.Arguments[0].NodeType == (ExpressionType)SqlExpressionType.ObjectReference))
			{
				Expression retval = null;
				
				foreach (var value in this.GetPrimaryKeyElementalExpressions(functionCallExpression.Arguments[0]))
				{
					var current = new SqlFunctionCallExpression(functionCallExpression.Type, functionCallExpression.Function, value);

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

			return base.VisitFunctionCall(functionCallExpression);
		}

		protected override Expression VisitBinary(BinaryExpression binaryExpression)
		{
			if (inProjector)
			{
				return binaryExpression;
			}

			if ((binaryExpression.Left.NodeType == (ExpressionType)SqlExpressionType.ObjectReference || binaryExpression.Left.NodeType == ExpressionType.MemberInit)
				&& (binaryExpression.Right.NodeType == (ExpressionType)SqlExpressionType.ObjectReference || binaryExpression.Right.NodeType == ExpressionType.MemberInit)
				&& (binaryExpression.Left.Type == binaryExpression.Right.Type))
			{
				Expression retval = null;
				var leftOperand = binaryExpression.Left;
				var rightOperand = binaryExpression.Right;

				foreach (var value in this.GetPrimaryKeyElementalExpressions(leftOperand)
					.Zip(this.GetPrimaryKeyElementalExpressions(rightOperand), (left, right) => new { Left = left, Right = right }))
				{
					Expression current;
					var left = this.Visit(value.Left);
					var right = this.Visit(value.Right);
					
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
