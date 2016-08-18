// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System.Collections.Generic;
using System.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Expressions
{
	public class SqlAlterTableExpression
		: SqlBaseExpression
	{
		public Expression Table { get; }
		public IReadOnlyList<Expression> Actions { get; }
		public IReadOnlyList<SqlConstraintActionExpression> ConstraintActions { get; }
		public override ExpressionType NodeType => (ExpressionType)SqlExpressionType.AlterTable;

		public SqlAlterTableExpression(Expression table, params SqlConstraintActionExpression[] constraintActions)
			: this(table, (IEnumerable<SqlConstraintActionExpression>)constraintActions)
		{	
		}

		public SqlAlterTableExpression(Expression table, IEnumerable<SqlConstraintActionExpression> constraintActions)
			: this(table, constraintActions.ToReadOnlyCollection())
		{
			
		}

		public SqlAlterTableExpression(Expression table, IReadOnlyList<SqlConstraintActionExpression> constraintActions)
			: base(typeof(void))
		{
			this.Table = table;
			this.ConstraintActions = constraintActions;
		}

		public SqlAlterTableExpression(Expression table, params Expression[] actions)
			: this(table, (IEnumerable<Expression>)actions)
		{
		}

		public SqlAlterTableExpression(Expression table, IEnumerable<Expression> actions)
			: this(table, actions.ToReadOnlyCollection())
		{

		}

		public SqlAlterTableExpression(Expression table, IReadOnlyList<Expression> actions)
			: base(typeof(void))
		{
			this.Table = table;
			this.Actions = actions;
		}
	}
}
