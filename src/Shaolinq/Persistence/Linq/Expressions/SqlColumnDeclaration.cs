// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

﻿using System.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Expressions
{
	/// <summary>
	/// A SqlColumnDeclaraction represents the part in parenthesis in the following select statement: SELECT (expression as columnname).
	/// </summary>
	public class SqlColumnDeclaration
	{
		/// <summary>
		/// The alias/name of the column declaration
		/// </summary>
		public string Name { get; }

		/// <summary>
		/// A SqlColumnExpression, SqlAggregateExpression or SqlOrderExpression.
		/// </summary>
		public Expression Expression { get; }

		/// <summary>
		/// Constructs a new <see cref="SqlColumnDeclaration"/>
		/// </summary>
		/// <param name="name">The name of the declaraction as it appears after the SELECT</param>
		/// <param name="expression">The expression (the source of the value for the declaration)</param>
		public SqlColumnDeclaration(string name, Expression expression)
		{
			this.Name = name;
			this.Expression = expression;
		}

		public SqlColumnDeclaration ReplaceExpression(Expression expression)
		{
			return new SqlColumnDeclaration(this.Name, expression);
		}
	}
}
