// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq.Expressions;

namespace Shaolinq.Persistence.Sql.Linq.Expressions
{
	public class SqlColumnDefinitionExpression
		: SqlBaseExpression
	{
		public string ColumnName { get; private set; }
		public ReadOnlyCollection<Expression> ConstraintExpressions { get; private set; }

		public SqlColumnDefinitionExpression(string columnName, IList<Expression> constraintExpressions)
			: base(typeof(void))
		{
			this.ColumnName = columnName;
			this.ConstraintExpressions = new ReadOnlyCollection<Expression>(constraintExpressions);
		}
	}
}
