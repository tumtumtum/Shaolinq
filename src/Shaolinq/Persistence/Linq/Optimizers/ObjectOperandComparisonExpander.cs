// Copyright (c) 2007-2014 Thong Nguyen (tumtumtum@gmail.com)

﻿using System;
﻿using System.Linq;
﻿using System.Linq.Expressions;
using Shaolinq.Persistence.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Optimizers
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
		private readonly DataAccessModel model;
		
		private ObjectOperandComparisonExpander(DataAccessModel model)
		{
			this.model = model;
		}

		public static Expression Expand(DataAccessModel model, Expression expression)
		{
			var fixer = new ObjectOperandComparisonExpander(model);

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
				return new SqlProjectionExpression(source, projector, aggregator, projection.IsElementTableProjection, projection.SelectFirstType, projection.DefaultValueExpression, projection.IsDefaultIfEmpty);
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
				if (functionCallExpression.Arguments[0] is SqlObjectReference)
				{
					Expression retval = null;
					var operand = (SqlObjectReference)functionCallExpression.Arguments[0];

					foreach (var current in operand
						.Bindings
						.OfType<MemberAssignment>()
						.Select(c => c.Expression)
						.Select(c => new SqlFunctionCallExpression(functionCallExpression.Type, functionCallExpression.Function, c)))
					{
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

			if (binaryExpression.Left.NodeType == (ExpressionType)SqlExpressionType.ObjectReference
				&& binaryExpression.Right.NodeType == (ExpressionType)SqlExpressionType.ObjectReference)
			{
				Expression retval = null;
				var leftOperand = (SqlObjectReference)binaryExpression.Left;
				var rightOperand = (SqlObjectReference)binaryExpression.Right;

				foreach (var value in leftOperand.GetBindingsFlattened().OfType<MemberAssignment>().Zip(rightOperand.GetBindingsFlattened().OfType<MemberAssignment>(), (left, right) => new { Left = left, Right = right }))
				{
					Expression current;
					var left = this.Visit(value.Left.Expression);
					var right = this.Visit(value.Right.Expression);
					
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
