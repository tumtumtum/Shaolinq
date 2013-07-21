using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Shaolinq.Persistence.Sql.Linq.Expressions;
using Platform;

namespace Shaolinq.Persistence.Sql.Linq.Optimizer
{
	/// <summary>
	/// An optimizer that turns nested SQL function calls into a single function call (if possible)
	/// function call.
	/// </summary>
	/// <remarks>
	/// For example: CONCAT(CONCAT(X, Y), Z) => CONCAT(X, Y, Z)
	/// </remarks>
	public class FunctionCoalescer
		: SqlExpressionVisitor
	{
		private FunctionCoalescer()
		{
		}

		public static Expression Coalesce(Expression expression)
		{
			var functionCoalescer = new FunctionCoalescer();

			return functionCoalescer.Visit(expression);
		}

		protected override Expression VisitBinary(BinaryExpression binaryExpression)
		{
			if (binaryExpression.NodeType == ExpressionType.NotEqual
				|| binaryExpression.NodeType == ExpressionType.Equal)
			{
				var function = binaryExpression.NodeType == ExpressionType.NotEqual ? SqlFunction.IsNotNull : SqlFunction.IsNull;

				var leftConstantExpression = binaryExpression.Left as ConstantExpression;
				var rightConstantExpression = binaryExpression.Right as ConstantExpression;

				if (rightConstantExpression != null)
				{
					if (rightConstantExpression.Value == null)
					{
						if (leftConstantExpression == null || leftConstantExpression.Value != null)
						{
							return new SqlFunctionCallExpression(binaryExpression.Type, function, binaryExpression.Left);
						}
					}
				}
				
				if (leftConstantExpression != null)
				{
					if (leftConstantExpression.Value == null)
					{
						if (rightConstantExpression == null || rightConstantExpression.Value != null)
						{
							return new SqlFunctionCallExpression(binaryExpression.Type, function, binaryExpression.Right);
						}
					}
				}
			}
			return base.VisitBinary(binaryExpression);
		}

		protected override Expression VisitFunctionCall(SqlFunctionCallExpression functionCallExpression)
		{
			if (functionCallExpression.Arguments.Count == 2)
			{
				if (functionCallExpression.Function == SqlFunction.Concat)
				{
					SqlFunctionCallExpression retval;

					var arg1 = functionCallExpression.Arguments[0] as SqlFunctionCallExpression;
					var arg2 = functionCallExpression.Arguments[1] as SqlFunctionCallExpression;

					if (arg1 == null && arg2 != null)
					{
						// Concat(something, Concat(?, ?))

						var arg1Args = Visit(functionCallExpression.Arguments[0]);
						var arg2Args = new List<Expression>();

						foreach (var arg in arg2.Arguments)
						{
							arg2Args.Add(Visit(arg));
						}

						retval = new SqlFunctionCallExpression(functionCallExpression.Type, SqlFunction.Concat, arg2Args.ToArray().Prepend(arg1Args));

						return retval;
					}
					else if (arg1 != null && arg2 == null)
					{
						// Concat(Concat(?, ?), something)

						var arg2Args = Visit(functionCallExpression.Arguments[1]);
						var arg1Args = new List<Expression>();

						foreach (var arg in arg1.Arguments)
						{
							arg1Args.Add(Visit(arg));
						}

						retval = new SqlFunctionCallExpression(functionCallExpression.Type, SqlFunction.Concat, arg1Args.ToArray().Append(arg2Args));

						return retval;
					}
					else if (arg1 != null && arg2 != null)
					{
						// Concat(Concat(?, ?), Concat(?, ?))

						var arg1Args = new List<Expression>();

						foreach (var arg in arg1.Arguments)
						{
							arg1Args.Add(Visit(arg));
						}

						var arg2Args = new List<Expression>();

						foreach (var arg in arg2.Arguments)
						{
							arg2Args.Add(Visit(arg));
						}

						retval = new SqlFunctionCallExpression(functionCallExpression.Type, SqlFunction.Concat, (arg1Args.Append(arg2Args)).ToArray());

						return retval;
					}
				}
			}

			return base.VisitFunctionCall(functionCallExpression);
		}
	}
}
