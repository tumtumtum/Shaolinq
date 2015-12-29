// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Shaolinq.Persistence.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Optimizers
{
	/// <summary>
	/// Converts binary expressions between two <see cref="SqlObjectReferenceExpression"/> expressions
	/// into multiple binary expressions performing the operation over the the primary
	/// keys of the object operands.
	/// </summary>
	public class SqlObjectOperandComparisonExpander
		: SqlExpressionVisitor
	{
		private bool inProjector;
		
		private SqlObjectOperandComparisonExpander()
		{
		}

		public static Expression Expand(Expression expression)
		{
			var expander = new SqlObjectOperandComparisonExpander();

			return expander.Visit(expression);
		}

		protected override Expression VisitProjection(SqlProjectionExpression projection)
		{
			var source = (SqlSelectExpression) this.Visit(projection.Select);

			var oldInProjector = this.inProjector;

			this.inProjector = true;

			Expression projector;

			try
			{
				projector = this.Visit(projection.Projector);
			}
			finally
			{
				this.inProjector = oldInProjector;
			}

			var aggregator = (LambdaExpression) this.Visit(projection.Aggregator);

			if (source != projection.Select
				|| projector != projection.Projector
				|| aggregator != projection.Aggregator)
			{
				return new SqlProjectionExpression(source, projector, aggregator, projection.IsElementTableProjection, projection.DefaultValueExpression, projection.IsDefaultIfEmpty);
			}

			return projection;
		}

		internal static IEnumerable<Expression> GetPrimaryKeyElementalExpressions(Expression expression)
		{
			var initExpression = expression as MemberInitExpression;

			if (initExpression != null)
			{
				var memberInitExpression = initExpression;

				foreach (var value in memberInitExpression
					.Bindings
					.OfType<MemberAssignment>()
					.Where(c => c.Member is PropertyInfo)
					.Where(binding => PropertyDescriptor.IsPropertyPrimaryKey((PropertyInfo)binding.Member))
					.Select(c => c.Expression))
				{
					if (value is MemberInitExpression || value is SqlObjectReferenceExpression)
					{
						foreach (var inner in GetPrimaryKeyElementalExpressions(value))
						{
							yield return inner;	
						}
					}
					else
					{
						yield return value;
					}
				}

				yield break;
			}

			var referenceExpression = expression as SqlObjectReferenceExpression;

			if (referenceExpression == null)
			{
				yield break;
			}

			var operand = referenceExpression;

			foreach (var value in operand
				.GetBindingsFlattened()
				.OfType<MemberAssignment>()
				.Select(c => c.Expression))
			{
				if (value is MemberInitExpression || value is SqlObjectReferenceExpression)
				{
					foreach (var inner in GetPrimaryKeyElementalExpressions(value))
					{
						yield return inner;
					}
				}
				else
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
				
				foreach (var value in GetPrimaryKeyElementalExpressions(functionCallExpression.Arguments[0]))
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
			if (this.inProjector)
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

				foreach (var value in GetPrimaryKeyElementalExpressions(this.Visit(leftOperand))
					.Zip(GetPrimaryKeyElementalExpressions(this.Visit(rightOperand)), (left, right) => new { Left = left, Right = right }))
				{
					Expression current;
					var left = value.Left;
					var right = value.Right;
					
					switch (binaryExpression.NodeType)
					{
						case ExpressionType.Equal:
							current = Expression.Equal(left, right);
							break;
						case ExpressionType.NotEqual:
							current = Expression.NotEqual(left, right);
							break;
						default:
							throw new NotSupportedException($"Operation on DataAccessObject with {binaryExpression.NodeType.ToString()} not supported");
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
