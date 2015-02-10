using System.Linq.Expressions;
using Shaolinq.Persistence.Linq.Expressions;

namespace Shaolinq.SqlServer
{
	public class SqlServerDateTimeFunctionsAmmender
		: SqlExpressionVisitor
	{
		public static Expression Ammend(Expression expression)
		{
			var ammender = new SqlServerDateTimeFunctionsAmmender();

			return ammender.Visit(expression);
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
