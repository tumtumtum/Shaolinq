// Copyright (c) 2007-2014 Thong Nguyen (tumtumtum@gmail.com)

﻿using System;
﻿using System.Collections.Generic;
﻿using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Expressions
{
	public class SqlTupleExpression
		: SqlBaseExpression
	{
		public ReadOnlyCollection<Expression> SubExpressions { get; private set; }

		internal protected static Type GetTupleExpressionType(IEnumerable<Expression> subExpressions)
		{
			var types = subExpressions.Select(c => c.Type).ToArray();

			var genericTupleType = Type.GetType("Shaolinq.MutableTuple`" + types.Length);

			return genericTupleType.MakeGenericType(types);
		}


		public SqlTupleExpression(IEnumerable<Expression> subExpressions)
			: this(subExpressions, null)
		{
		}

		public SqlTupleExpression(IEnumerable<Expression> subExpressions, Type type)
			: this(new ReadOnlyCollection<Expression>(subExpressions.ToList()), type)
		{
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