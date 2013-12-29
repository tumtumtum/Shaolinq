// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

using System.Linq.Expressions;
using Shaolinq.Persistence.Sql.Linq.Expressions;

namespace Shaolinq.Persistence.Sql.Linq
{
	public abstract class SqlQueryFormatter
		: SqlExpressionVisitor
	{	
		public abstract SqlQueryFormatResult Format();

		protected virtual Expression PreProcess(Expression expression)
		{
			return expression;
		}
	}
}
