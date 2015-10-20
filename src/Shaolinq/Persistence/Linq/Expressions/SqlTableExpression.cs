// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

﻿using System;
using System.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Expressions
{
	public class SqlTableExpression
		: SqlBaseExpression
	{
		public string Name { get; }
		public string Alias { get; }
		public override ExpressionType NodeType => (ExpressionType)SqlExpressionType.Table;

		public SqlTableExpression(string name)
			: base(typeof(void))
		{
			this.Name = name;
			this.Alias = null;
		}

		public SqlTableExpression(Type type, string alias, string name)
			: base(type)
		{
			this.Name = name;
			this.Alias = alias;
		}

		public override string ToString()
		{
			return this.GetType().Name + ":" + new Sql92QueryFormatter().Format(this).CommandText;
		}
	}
}