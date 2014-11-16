// Copyright (c) 2007-2014 Thong Nguyen (tumtumtum@gmail.com)

﻿using System;
﻿using System.Collections.Generic;
﻿using System.Linq.Expressions;
﻿using Platform.Collections;

namespace Shaolinq.Persistence.Linq.Expressions
{
	/// <summary>
	/// Represents an SQL function call such as DATE, YEAR, SUBSTRING or ISNULL.
	/// </summary>
	public class SqlFunctionCallExpression
		: SqlBaseExpression
	{
		public SqlFunction Function { get; private set; }
		public string UserDefinedFunctionName { get; private set; }
		public IReadOnlyList<Expression> Arguments { get; private set; }
		public override ExpressionType NodeType { get { return (ExpressionType)SqlExpressionType.FunctionCall; } }

		public SqlFunctionCallExpression(Type type, SqlFunction function, params Expression[] arguments)
			: this(type, function, arguments.ToReadOnlyList())
		{
		}

		public SqlFunctionCallExpression(Type type, string userDefinedFunctionName, params Expression[] arguments)
			: this(type, SqlFunction.UserDefined, arguments.ToReadOnlyList())
		{
			this.UserDefinedFunctionName = userDefinedFunctionName;
		}

		public SqlFunctionCallExpression(Type type, SqlFunction function, IEnumerable<Expression> arguments)
			: this(type, function, arguments.ToReadOnlyList())
		{
		}

		public SqlFunctionCallExpression(Type type, string userDefinedFunctionName, IEnumerable<Expression> arguments)
			: this(type, SqlFunction.UserDefined, arguments.ToReadOnlyList())
		{
			this.UserDefinedFunctionName = userDefinedFunctionName;
		}

		public SqlFunctionCallExpression(Type type, SqlFunction function, IReadOnlyList<Expression> arguments)
			: base(type)
		{
			this.Function = function;
			this.Arguments = arguments;
		}
	}
}
