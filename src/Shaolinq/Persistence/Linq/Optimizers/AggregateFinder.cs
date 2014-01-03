using System.Collections.Generic;
using System.Linq.Expressions;
using Shaolinq.Persistence.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Optimizers
{
	public class AggregateFinder
		: SqlExpressionVisitor
	{
		private readonly List<SqlAggregateExpression> aggregatesFound;
		
		private AggregateFinder()
		{
			this.aggregatesFound = new List<SqlAggregateExpression>();
		}

		public static List<SqlAggregateExpression> Find(Expression expression)
		{
			var gatherer = new AggregateFinder();

			gatherer.Visit(expression);

			return gatherer.aggregatesFound;
		}

		protected override Expression VisitAggregate(SqlAggregateExpression aggregate)
		{
			this.aggregatesFound.Add(aggregate);

			return base.VisitAggregate(aggregate);
		}
	}
}
