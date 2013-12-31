// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

﻿using System;
using System.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Expressions
{
	/// <summary>
	/// Represents an aggregate subquery before the aggregate has been rewritten.
	/// </summary>
	/// <remarks>
	/// When a LINQ query projects a column using an aggregate, the subquery holds a select expression that represents that single aggregate call.
	/// The subquery consists of the part inside the outer parenthesis: <c>SELECT (SELECT MAX(id) FROM product) AS max FROM product;</c>
	/// </remarks>
	public class SqlSubqueryExpression
		: SqlBaseExpression
	{
		public SqlSelectExpression Select { get; private set; }

		public override ExpressionType NodeType
		{
			get
			{
				return (ExpressionType)SqlExpressionType.Subquery;
			}
		}

		public SqlSubqueryExpression(Type type, SqlSelectExpression select)
			: base(type)
		{
			this.Select = select;
		}
	}
}