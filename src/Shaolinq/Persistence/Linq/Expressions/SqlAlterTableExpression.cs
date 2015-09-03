// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System.Collections.Generic;
using System.Linq.Expressions;
using Platform.Collections;

namespace Shaolinq.Persistence.Linq.Expressions
{
	public class SqlAlterTableExpression
		: SqlBaseExpression
	{
		public Expression Table { get; private set; }
		public IReadOnlyList<SqlConstraintActionExpression> Actions { get; private set; }
		public override ExpressionType NodeType { get { return (ExpressionType)SqlExpressionType.AlterTable; } }

		public SqlAlterTableExpression(Expression table, params SqlConstraintActionExpression[] actions)
			: this(table, (IEnumerable<SqlConstraintActionExpression>)actions)
		{	
		}

		public SqlAlterTableExpression(Expression table, IEnumerable<SqlConstraintActionExpression> actions)
			: this(table, actions.ToReadOnlyList())
		{
			
		}
		public SqlAlterTableExpression(Expression table, IReadOnlyList<SqlConstraintActionExpression> actions)
			: base(typeof(void))
		{
			this.Table = table;
			this.Actions = actions;
		}
	}
}
