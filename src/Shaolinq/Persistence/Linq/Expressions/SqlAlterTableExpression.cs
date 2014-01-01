// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

using System.Collections.ObjectModel;
using System.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Expressions
{
	public class SqlAlterTableExpression
		: SqlBaseExpression
	{
		public override ExpressionType NodeType
		{
			get
			{
				return (ExpressionType)SqlExpressionType.AlterTable;
			}
		}

		public Expression Table { get; private set; }
		public ReadOnlyCollection<Expression> Actions { get; private set; }

		public SqlAlterTableExpression(Expression table, ReadOnlyCollection<Expression> actions)
			: base(typeof(void))
		{
			this.Table = table;
			this.Actions = actions;
		}
	}
}
