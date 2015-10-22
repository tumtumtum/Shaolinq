// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System.Collections.Generic;
using System.Linq.Expressions;
using Platform.Collections;

namespace Shaolinq.Persistence.Linq.Expressions
{
	public class SqlAlterTableExpression
		: SqlBaseExpression
	{
		public Expression Table { get; }
		public IReadOnlyList<SqlConstraintActionExpression> Actions { get; }
		public override ExpressionType NodeType => (ExpressionType)SqlExpressionType.AlterTable;

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
