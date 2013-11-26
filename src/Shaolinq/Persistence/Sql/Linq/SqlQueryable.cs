// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

﻿using System.Collections.Generic;
using System.Linq.Expressions;

namespace Shaolinq.Persistence.Sql.Linq
{
	public class SqlQueryable<T>
		: ReusableQueryable<T>
	{
		public SqlQueryable(SqlQueryProvider provider, Expression expression)
			: base(provider, expression)
		{
		}

		public override IEnumerator<T> GetEnumerator()
		{
			return ((IEnumerable<T>)this.Provider.Execute(this.Expression)).GetEnumerator();
		}
	}
}
