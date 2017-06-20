using System.Linq.Expressions;
using Platform;
using Shaolinq.Persistence.Linq;
using Shaolinq.Persistence.Linq.Expressions;

namespace Shaolinq.SqlServer
{
	public class SqlServerAggregateTypeFixer
		: SqlExpressionVisitor
	{
		public static Expression Fix(Expression expression)
		{
			return new SqlServerAggregateTypeFixer().Visit(expression);
		}

		protected override Expression VisitAggregate(SqlAggregateExpression sqlAggregate)
		{
			if (sqlAggregate.AggregateType == SqlAggregateType.Average && sqlAggregate.Argument.Type.IsIntegerType())
			{
				return sqlAggregate.ChangeArgument(Expression.Convert(sqlAggregate.Argument, typeof(double)));
			}

			return base.VisitAggregate(sqlAggregate);
		}
	}
}