// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Linq.Expressions;
using Shaolinq.Persistence.Linq.Expressions;

namespace Shaolinq.Persistence.Linq
{
	public class SqlConstantPlaceholderMaxIndexFinder
		: SqlExpressionVisitor
	{
		private int maxIndex = 0;
		
		private SqlConstantPlaceholderMaxIndexFinder()
		{
		}

		protected override Expression VisitConstantPlaceholder(SqlConstantPlaceholderExpression constantPlaceholder)
		{
			this.maxIndex = Math.Max(constantPlaceholder.Index, this.maxIndex);

			return base.VisitConstantPlaceholder(constantPlaceholder);
		}

		public static int Find(Expression expression)
		{
			var finder = new SqlConstantPlaceholderMaxIndexFinder();

			finder.Visit(expression);

			return finder.maxIndex;
		}
	}
}