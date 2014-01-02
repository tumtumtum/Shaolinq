// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

using System.Linq.Expressions;
using Shaolinq.Persistence.Linq;

namespace Shaolinq.Persistence
{
	public abstract class SqlQueryFormatterManager
	{
		public abstract SqlQueryFormatter CreateQueryFormatter(SqlQueryFormatterOptions options = SqlQueryFormatterOptions.Default);
		
		public virtual SqlQueryFormatResult Format(Expression expression, SqlQueryFormatterOptions options = SqlQueryFormatterOptions.Default)
		{
			return CreateQueryFormatter(options).Format(expression);
		}
	}
}
