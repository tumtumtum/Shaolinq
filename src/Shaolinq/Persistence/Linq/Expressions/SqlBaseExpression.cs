// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Expressions
{
	public abstract class SqlBaseExpression
		: Expression
	{
		public override Type Type { get; }
		
		protected SqlBaseExpression(Type type)
		{
			this.Type = type;
		}

		public override string ToString()
		{
			return new Sql92QueryFormatter().Format(this).CommandText;
		}
	}
}
