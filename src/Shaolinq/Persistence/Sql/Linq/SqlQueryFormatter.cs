// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

﻿using Shaolinq.Persistence.Sql.Linq.Expressions;

namespace Shaolinq.Persistence.Sql.Linq
{
	public abstract class SqlQueryFormatter
		: SqlExpressionVisitor
	{
		public abstract SqlQueryFormatResult Format();
	}
}
