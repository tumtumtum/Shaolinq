// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Linq;
using System.Linq.Expressions;
using Shaolinq.Persistence.Linq;

namespace Shaolinq.Persistence
{
	public class DefaultSqlQueryFormatterManager
		: SqlQueryFormatterManager
	{
		public SqlDataTypeProvider SqlDataTypeProvider { get; set; }
		public SqlDialect SqlDialect { get; set; }
		public SqlQueryFormatterConstructorMethod ConstructorMethod { get; private set; }
		public delegate SqlQueryFormatter SqlQueryFormatterConstructorMethod(SqlQueryFormatterOptions options, SqlDialect sqlDialect, SqlDataTypeProvider sqlDataTypeProvider);

		private static SqlQueryFormatterConstructorMethod CreateConstructorMethodFromType(Type type)
		{
			var options = Expression.Parameter(typeof(SqlQueryFormatterOptions), "options");
			var sqlDialect = Expression.Parameter(typeof(SqlDialect), "sqlDialect");
			var sqlDataTypeProvider = Expression.Parameter(typeof(SqlDataTypeProvider), "sqlDataTypeProvider");

			var parameters = new[] { options, sqlDialect, sqlDataTypeProvider };

			var constructor = type.GetConstructor(parameters.Select(c => c.Type).ToArray());

			var newExpression = Expression.New(constructor, parameters);

			return Expression.Lambda<SqlQueryFormatterConstructorMethod>(newExpression, parameters).Compile();
		}

		public DefaultSqlQueryFormatterManager(SqlDialect sqlDialect, SqlDataTypeProvider sqlDataTypeProvider, Type sqlFormatterType)
			: this(sqlDialect, sqlDataTypeProvider, CreateConstructorMethodFromType(sqlFormatterType))
		{
		}

		public DefaultSqlQueryFormatterManager(SqlDialect sqlDialect, SqlDataTypeProvider sqlDataTypeProvider, SqlQueryFormatterConstructorMethod constructorMethod)
			: base(sqlDialect)
		{
			this.SqlDialect = sqlDialect;
			this.SqlDataTypeProvider = sqlDataTypeProvider;			
			this.ConstructorMethod = constructorMethod;
		}

		public override SqlQueryFormatter CreateQueryFormatter(SqlQueryFormatterOptions options = SqlQueryFormatterOptions.Default)
		{
			return this.ConstructorMethod(options, this.SqlDialect, this.SqlDataTypeProvider);
		}
	}
}
