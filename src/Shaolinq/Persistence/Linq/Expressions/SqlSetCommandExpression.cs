// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System.Linq.Expressions;
using Platform.Collections;

namespace Shaolinq.Persistence.Linq.Expressions
{
	public class SqlSetCommandExpression
		: SqlBaseExpression
	{
		public string ConfigurationParameter { get; private set; }
		public Expression  Target { get; private set; }
		public IReadOnlyList<Expression> Arguments { get; private set; }
		public override ExpressionType NodeType { get { return (ExpressionType)SqlExpressionType.SetCommand; } }

		public SqlSetCommandExpression(string configurationParameter, Expression target, params Expression[] arguments)
			: this(configurationParameter, target, arguments.ToReadOnlyList())
		{
		}

		public SqlSetCommandExpression(string configurationParameter, Expression target, IReadOnlyList<Expression> arguments)
			: base(typeof(void))
		{
			this.ConfigurationParameter = configurationParameter;
			this.Target = target;
			this.Arguments = arguments.ToReadOnlyList();
		}
	}
}
