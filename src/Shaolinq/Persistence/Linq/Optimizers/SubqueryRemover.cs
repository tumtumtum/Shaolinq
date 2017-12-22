// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Shaolinq.Persistence.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Optimizers
{
	public class SubqueryRemover
		: SqlExpressionVisitor
	{
		private readonly HashSet<SqlSelectExpression> selectsToRemove;
		private readonly Dictionary<string, Dictionary<string, Expression>> columnsBySelectAliasByColumnName;

		private SubqueryRemover(IEnumerable<SqlSelectExpression> selectsToRemove)
		{
			this.selectsToRemove = new HashSet<SqlSelectExpression>(selectsToRemove);
			
			this.columnsBySelectAliasByColumnName = this.selectsToRemove.ToDictionary(d => d.Alias, d => d.Columns.ToDictionary(d2 => d2.Name, d2 => d2.Expression));
		}

		public static SqlSelectExpression Remove(SqlSelectExpression outerSelect, params SqlSelectExpression[] selectsToRemove)
		{
			return Remove(outerSelect, (IEnumerable<SqlSelectExpression>)selectsToRemove);
		}

		public static SqlSelectExpression Remove(SqlSelectExpression outerSelect, IEnumerable<SqlSelectExpression> selectsToRemove)
		{
			return (SqlSelectExpression)new SubqueryRemover(selectsToRemove).Visit(outerSelect);
		}

		public static SqlProjectionExpression Remove(SqlProjectionExpression projection, params SqlSelectExpression[] selectsToRemove)
		{
			return Remove(projection, (IEnumerable<SqlSelectExpression>)selectsToRemove);
		}

		public static SqlProjectionExpression Remove(SqlProjectionExpression projection, IEnumerable<SqlSelectExpression> selectsToRemove)
		{
			return (SqlProjectionExpression)new SubqueryRemover(selectsToRemove).Visit(projection);
		}

		protected override Expression VisitSelect(SqlSelectExpression select)
		{
			if (this.selectsToRemove.Contains(select))
			{
				return this.Visit(select.From);
			}
			else
			{
				return base.VisitSelect(select);
			}
		}

		protected override Expression VisitColumn(SqlColumnExpression column)
		{
			if (this.columnsBySelectAliasByColumnName.TryGetValue(column.SelectAlias, out var columnsByName))
			{
				if (columnsByName.TryGetValue(column.Name, out var expr))
				{
					return this.Visit(expr);
				}

				throw new InvalidOperationException("Reference to undefined column: " + column.AliasedName);
			}

			return column;
		}
	}
}
