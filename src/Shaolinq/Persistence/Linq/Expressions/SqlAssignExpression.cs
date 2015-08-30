// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Expressions
{
	public class SqlAssignExpression
		: SqlBaseExpression
	{
		public Expression Target { get; private set; }
		public Expression Value { get; private set; }
		public override ExpressionType NodeType { get { return (ExpressionType)SqlExpressionType.Assign; } }

		public SqlAssignExpression(Expression target, Expression value)
			: base(target.Type)
		{
			this.Target = target;
			this.Value = value;
		}
	}
}
