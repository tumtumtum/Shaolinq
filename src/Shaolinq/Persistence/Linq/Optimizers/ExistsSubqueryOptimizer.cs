using System.Linq.Expressions;
using Shaolinq.Persistence.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Optimizers
{
	public class ExistsSubqueryOptimizer
		: SqlExpressionVisitor
	{
		private ExistsSubqueryOptimizer()
		{
		}

		public static Expression Optimize(Expression expression)
		{
			return new ExistsSubqueryOptimizer().Visit(expression);
		}

		protected override Expression VisitFunctionCall(SqlFunctionCallExpression functionCallExpression)
		{
			if (functionCallExpression.Function == SqlFunction.In)
			{
				if (functionCallExpression.Arguments[0].NodeType == ExpressionType.Constant && functionCallExpression.Arguments[1].NodeType == (ExpressionType)SqlExpressionType.Projection)
				{
					var projection = (SqlProjectionExpression)functionCallExpression.Arguments[1];

					if (projection.Select.Where == null &&  projection.Select.Columns.Count == 1)
					{
						var newWhere = Expression.Equal(functionCallExpression.Arguments[0], projection.Select.Columns[0].Expression);
						var newSelect = new SqlSelectExpression(projection.Select.Type, projection.Select.Alias, projection.Select.Columns, projection.Select.From, newWhere, projection.Select.OrderBy, projection.Select.ForUpdate);
						var newProjection = new SqlProjectionExpression(newSelect, projection, projection.Aggregator);

						return new SqlFunctionCallExpression(functionCallExpression.Type, SqlFunction.Exists, new [] { newProjection });
					}
				}
			}
			return base.VisitFunctionCall(functionCallExpression);
		}
	}
}
