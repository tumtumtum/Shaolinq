// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Linq;
using System.Linq.Expressions;
using Platform;
using Shaolinq.Persistence.Linq;

namespace Shaolinq.Persistence
{
	public class DefaultSqlQueryFormatterManager
		: SqlQueryFormatterManager
	{
		public SqlDialect SqlDialect { get; }
		public SqlQueryFormatterConstructorMethod ConstructorMethod { get; }
		public delegate SqlQueryFormatter SqlQueryFormatterConstructorMethod(SqlQueryFormatterOptions options);

		public DefaultSqlQueryFormatterManager(SqlDialect sqlDialect, SqlQueryFormatterConstructorMethod constructorMethod)
			: base(sqlDialect)
		{
			this.SqlDialect = sqlDialect;
			this.ConstructorMethod = constructorMethod;
		}

		public override SqlQueryFormatter CreateQueryFormatter(SqlQueryFormatterOptions options = SqlQueryFormatterOptions.Default)
		{
			return this.ConstructorMethod(options);
		}
	}
}
