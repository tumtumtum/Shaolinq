// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System.Linq.Expressions;
using Shaolinq.Persistence.Linq;
using Shaolinq.Persistence.Linq.Expressions;

namespace Shaolinq.SqlServer
{
	public class SqlServerDateTimeFunctionsAmender
		: SqlExpressionVisitor
	{
		public static Expression Amend(Expression expression)
		{
			var amender = new SqlServerDateTimeFunctionsAmender();

			return amender.Visit(expression);
		}

		protected override Expression VisitFunctionCall(SqlFunctionCallExpression functionCallExpression)
		{
			switch (functionCallExpression.Function)
			{
			case SqlFunction.TimeSpanFromDays:
				return Expression.MakeBinary(ExpressionType.Multiply, Expression.Constant(24 * 60 * 60.0 * 10000000.0), Expression.Convert(this.Visit(functionCallExpression.Arguments[0]), typeof(double)));
			case SqlFunction.TimeSpanFromHours:
				return this.Visit(Expression.MakeBinary(ExpressionType.Multiply, Expression.Constant(60.0 * 60.0 * 10000000.0), Expression.Convert(this.Visit(functionCallExpression.Arguments[0]), typeof(double))));
			case SqlFunction.TimeSpanFromMinutes:
				return this.Visit(Expression.MakeBinary(ExpressionType.Multiply, Expression.Constant(60.0 * 10000000.0), Expression.Convert(this.Visit(functionCallExpression.Arguments[0]), typeof(double))));
			case SqlFunction.TimeSpanFromSeconds:
				return this.Visit(Expression.MakeBinary(ExpressionType.Multiply, Expression.Constant(10000000.0), Expression.Convert(this.Visit(functionCallExpression.Arguments[0]), typeof(double))));
			}

			return base.VisitFunctionCall(functionCallExpression);
		}
	}
}
