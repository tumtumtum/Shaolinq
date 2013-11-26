// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

﻿using System;
using System.Linq.Expressions;

namespace Shaolinq.Persistence.Sql.Linq.Expressions
{
	public class SqlTableExpression
		: SqlBaseExpression
	{
		public string Name { get; private set; }
		public string Alias { get; private set; }

		public override ExpressionType NodeType
		{
			get
			{
				return (ExpressionType)SqlExpressionType.Table;
			}
		}

		public SqlTableExpression(Type type, string alias, string name)
			: base(type)
		{
			this.Name = name;
			this.Alias = alias;
		}

		public override string ToString()
		{
			return this.GetType().Name + ":" + new Sql92QueryFormatter(this).Format().CommandText;
		}
	}
}
