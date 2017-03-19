// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Shaolinq.Persistence.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Optimizers
{
	public class SqlConstantPlaceholderValuesCollector
		: SqlExpressionVisitor
	{
		private int maxIndex = -1;
		private readonly List<SqlConstantPlaceholderExpression> values = new List<SqlConstantPlaceholderExpression>();


		private SqlConstantPlaceholderValuesCollector()
		{
		}

		protected override Expression VisitConstantPlaceholder(SqlConstantPlaceholderExpression constantPlaceholder)
		{
			this.values.Add(constantPlaceholder);

			this.maxIndex = Math.Max(constantPlaceholder.Index, this.maxIndex);

			return base.VisitConstantPlaceholder(constantPlaceholder);
		}

		public static object[] CollectValues(Expression expression, Func<SqlConstantPlaceholderExpression, object> transform = null)
		{
			var collector = new SqlConstantPlaceholderValuesCollector();

			collector.Visit(expression);

			if (collector.values.Count == 0)
			{
				return new object[0];
			}

			var retval = new object[collector.maxIndex + 1];

			if (transform == null)
			{
				foreach (var item in collector.values)
				{
					retval[item.Index] = item.ConstantExpression.Value;
				}
			}
			else
			{
				foreach (var item in collector.values)
				{
					retval[item.Index] = transform(item);
				}
			}

			return retval;
		}
	}
}
