// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

using System.Linq.Expressions;
using Shaolinq.Persistence.Linq.Expressions;

namespace Shaolinq.Persistence.Linq
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
