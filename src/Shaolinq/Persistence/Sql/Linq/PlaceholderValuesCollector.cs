// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

﻿using System;
using System.Linq.Expressions;
using Shaolinq.Persistence.Sql.Linq.Expressions;

namespace Shaolinq.Persistence.Sql.Linq
{
	public class PlaceholderValuesCollector
		: SqlExpressionVisitor
	{
		private int maxLength = 0;
		private object[] values;

		public object[] Values
		{
			get
			{
				return values;
			}
		}

		private PlaceholderValuesCollector()
		{
			this.values = new object[32];
		}

		protected override Expression VisitConstantPlaceholder(SqlConstantPlaceholderExpression constantPlaceholder)
		{
			var newLength = this.values.Length;

			while (constantPlaceholder.Index > newLength - 1)
			{
				newLength *= 2;
			}

			if (newLength != this.values.Length)
			{
				Array.Resize(ref this.values, newLength);
			}

			values[constantPlaceholder.Index] = constantPlaceholder.ConstantExpression.Value;

			if (maxLength < constantPlaceholder.Index + 1)
			{
				maxLength = constantPlaceholder.Index + 1;
			}

			return base.VisitConstantPlaceholder(constantPlaceholder);
		}

		public static object[] CollectValues(Expression expression)
		{
			var collector = new PlaceholderValuesCollector();

			collector.Visit(expression);

			var retval = collector.Values;

			return retval;
		}
	}
}
