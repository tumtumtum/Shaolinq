// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Expressions
{
	public class SqlTableExpression
		: SqlAliasedExpression
	{
		public string Name { get; }
		public override ExpressionType NodeType => (ExpressionType)SqlExpressionType.Table;

		public SqlTableExpression(string name)
			: base(typeof(void), null)
		{
			this.Name = name;
		}

		public SqlTableExpression(Type type, string alias, string name)
			: base(type, alias)
		{
			this.Name = name;
		}

		public override string ToString()
		{
			return this.GetType().Name + ":" + new Sql92QueryFormatter().Format(this).CommandText;
		}
	}
}