// Copyright (c) 2007-2014 Thong Nguyen (tumtumtum@gmail.com)

using System.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Expressions
{
	public class SqlConstraintActionExpression
		: SqlBaseExpression
	{
		public override ExpressionType NodeType
		{
			get
			{
				return (ExpressionType)SqlExpressionType.ConstraintAction;
			}
		}

		public SqlConstraintActionType ActionType {get; private set;}
		public Expression ConstraintExpression { get; private set; }

		public SqlConstraintActionExpression(SqlConstraintActionType actionType, Expression constraintExpression)
			: base(typeof(void))
		{
			this.ActionType = actionType;
			this.ConstraintExpression = constraintExpression;
		}
	}
}
