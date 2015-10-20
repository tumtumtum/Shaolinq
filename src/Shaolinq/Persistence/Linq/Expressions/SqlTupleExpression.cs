// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

﻿using System;
﻿using System.Collections.Generic;
﻿using System.Linq;
using System.Linq.Expressions;
﻿using Platform.Collections;

namespace Shaolinq.Persistence.Linq.Expressions
{
	public class SqlTupleExpression
		: SqlBaseExpression
	{
		public IReadOnlyList<Expression> SubExpressions { get; }

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
			: this(subExpressions.ToReadOnlyList(), type)
		{
		}

		public SqlTupleExpression(IReadOnlyList<Expression> subExpressions)
			: this(subExpressions, null)
		{	
		}

		public SqlTupleExpression(IReadOnlyList<Expression> subExpressions, Type type)
			: base(type ?? GetTupleExpressionType(subExpressions))
		{
			this.SubExpressions = subExpressions;
		}
	}
}