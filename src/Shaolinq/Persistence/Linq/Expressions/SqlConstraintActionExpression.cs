// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Expressions
{
	public class SqlConstraintActionExpression
		: SqlBaseExpression
	{
		public SqlConstraintActionType ActionType {get; private set;}
		public Expression ConstraintExpression { get; }
		public override ExpressionType NodeType => (ExpressionType)SqlExpressionType.ConstraintAction;

		public SqlConstraintActionExpression(SqlConstraintActionType actionType, Expression constraintExpression)
			: base(typeof(void))
		{
			this.ActionType = actionType;
			this.ConstraintExpression = constraintExpression;
		}
	}
}
