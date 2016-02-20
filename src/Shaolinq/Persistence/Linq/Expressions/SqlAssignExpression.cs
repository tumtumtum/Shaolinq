// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Expressions
{
	public class SqlAssignExpression
		: SqlBaseExpression
	{
		public Expression Target { get; }
		public Expression Value { get; }
		public override ExpressionType NodeType => (ExpressionType)SqlExpressionType.Assign;

		public SqlAssignExpression(Expression target, Expression value)
			: base(target.Type)
		{
			this.Target = target;
			this.Value = value;
		}
	}
}
