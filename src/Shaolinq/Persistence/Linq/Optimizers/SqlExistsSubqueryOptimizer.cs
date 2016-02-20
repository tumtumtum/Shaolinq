// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System.Linq.Expressions;
using Shaolinq.Persistence.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Optimizers
{
	public class SqlExistsSubqueryOptimizer
		: SqlExpressionVisitor
	{
		private SqlExistsSubqueryOptimizer()
		{
		}

		public static Expression Optimize(Expression expression)
		{
			return new SqlExistsSubqueryOptimizer().Visit(expression);
		}

		protected override Expression VisitFunctionCall(SqlFunctionCallExpression functionCallExpression)
		{
			if (functionCallExpression.Function == SqlFunction.In)
			{
				if (functionCallExpression.Arguments[0].NodeType == ExpressionType.Constant && functionCallExpression.Arguments[1].NodeType == (ExpressionType)SqlExpressionType.Projection)
				{
					var projector = (SqlProjectionExpression)functionCallExpression.Arguments[1];

					if (projector.Select.Where == null &&  projector.Select.Columns.Count == 1)
					{
						var newWhere = Expression.Equal(functionCallExpression.Arguments[0], projector.Select.Columns[0].Expression);
						var newSelect = projector.Select.ChangeWhere(newWhere);
						var newProjection = new SqlProjectionExpression(newSelect, projector, projector.Aggregator);

						return new SqlFunctionCallExpression(functionCallExpression.Type, SqlFunction.Exists, newProjection);
					}
				}
			}

			return base.VisitFunctionCall(functionCallExpression);
		}
	}
}
