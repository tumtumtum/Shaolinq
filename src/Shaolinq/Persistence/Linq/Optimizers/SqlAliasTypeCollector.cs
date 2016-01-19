using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Shaolinq.Persistence.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Optimizers
{
	public class SqlAliasTypeCollector
		: SqlExpressionVisitor
	{
		private readonly HashSet<Tuple<string, Type>> results = new HashSet<Tuple<string, Type>>();

		private SqlAliasTypeCollector()
		{
		}

		public static List<Tuple<string, Type>> Collect(Expression expression)
		{
			var collector = new SqlAliasTypeCollector();
			
			collector.Visit(expression);

			return collector.results.ToList();
		}

		protected override Expression VisitTable(SqlTableExpression table)
		{
			results.Add(new Tuple<string, Type>(table.Alias, table.Type));

			return base.VisitTable(table);
		}

		protected override Expression VisitSelect(SqlSelectExpression selectExpression)
		{
			results.Add(new Tuple<string, Type>(selectExpression.Alias, selectExpression.Type));

			return base.VisitSelect(selectExpression);
		}
	}
}
