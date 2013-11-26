// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

﻿using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;

namespace Shaolinq.Persistence.Sql.Linq.Expressions
{
	public class SqlTupleExpression
		: SqlBaseExpression
	{
		public ReadOnlyCollection<Expression> SubExpressions { get; private set; }

		internal protected static Type GetTupleExpressionType(ReadOnlyCollection<Expression> subExpressions)
		{
			var types = subExpressions.Select(c => c.Type).ToArray();

			var genericTupleType = Type.GetType("Shaolinq.MutableTuple`" + types.Length);

			return genericTupleType.MakeGenericType(types);
		}

		public SqlTupleExpression(ReadOnlyCollection<Expression> subExpressions)
			: this(subExpressions, null)
		{	
		}

		public SqlTupleExpression(ReadOnlyCollection<Expression> subExpressions, Type type)
			: base(type ?? GetTupleExpressionType(subExpressions))
		{
			this.SubExpressions = subExpressions;
		}
	}
}
