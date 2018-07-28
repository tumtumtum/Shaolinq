// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)

using System.Collections.Generic;
using System.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Expressions
{
	public class SqlSetCommandExpression
		: SqlBaseExpression
	{
		public string ConfigurationParameter { get; }
		public Expression  Target { get; }
		public IReadOnlyList<Expression> Arguments { get; }
		public override ExpressionType NodeType => (ExpressionType)SqlExpressionType.SetCommand;

		public SqlSetCommandExpression(string configurationParameter, Expression target, params Expression[] arguments)
			: this(configurationParameter, target, arguments.ToReadOnlyCollection())
		{
		}

		public SqlSetCommandExpression(string configurationParameter, Expression target, IReadOnlyList<Expression> arguments)
			: base(typeof(void))
		{
			this.ConfigurationParameter = configurationParameter;
			this.Target = target;
			this.Arguments = arguments.ToReadOnlyCollection();
		}
	}
}
