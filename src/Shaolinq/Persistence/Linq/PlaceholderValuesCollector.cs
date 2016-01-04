// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System.Collections.Generic;
using System.Linq.Expressions;
using Shaolinq.Persistence.Linq.Expressions;

namespace Shaolinq.Persistence.Linq
{
	public class PlaceholderValuesCollector
		: SqlExpressionVisitor
	{
		private readonly List<object> values = new List<object>();

		private PlaceholderValuesCollector()
		{
		}

		protected override Expression VisitConstantPlaceholder(SqlConstantPlaceholderExpression constantPlaceholder)
		{
			this.values.Add(constantPlaceholder.ConstantExpression.Value);

			return base.VisitConstantPlaceholder(constantPlaceholder);
		}

		public static List<object> CollectValues(Expression expression)
		{
			var collector = new PlaceholderValuesCollector();

			collector.Visit(expression);

			var retval = collector.values;

			return retval;
		}
	}
}
