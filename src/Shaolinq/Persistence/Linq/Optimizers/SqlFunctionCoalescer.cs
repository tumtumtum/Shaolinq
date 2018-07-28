// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)

using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Platform;
using Shaolinq.Persistence.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Optimizers
{
	/// <summary>
	/// An optimizer that turns nested SQL function calls into a single function call (if possible)
	/// function call.
	/// </summary>
	/// <remarks>
	/// For example: CONCAT(CONCAT(X, Y), Z) => CONCAT(X, Y, Z)
	/// </remarks>
	public class SqlFunctionCoalescer
		: SqlExpressionVisitor
	{
		private SqlFunctionCoalescer()
		{
		}

		public static Expression Coalesce(Expression expression)
		{
			var functionCoalescer = new SqlFunctionCoalescer();

			return functionCoalescer.Visit(expression);
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

					if (arg1 == null && arg2 != null && arg2.Function == SqlFunction.Concat)
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
					else if (arg1 != null && arg2 == null && arg1.Function == SqlFunction.Concat)
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
					else if (arg1 != null && arg2 != null && arg1.Function == SqlFunction.Concat && arg2.Function == SqlFunction.Concat)
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

						retval = new SqlFunctionCallExpression(functionCallExpression.Type, SqlFunction.Concat, (arg1Args.Concat(arg2Args)).ToArray());

						return retval;
					}
				}
			}

			return base.VisitFunctionCall(functionCallExpression);
		}
	}
}
