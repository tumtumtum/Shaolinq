// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System.Collections.Generic;
using System.Linq.Expressions;
using Platform.Collections;

namespace Shaolinq.Persistence.Linq.Expressions
{
	public class SqlStatementListExpression
		: SqlBaseExpression
	{
		public IReadOnlyList<Expression> Statements { get; private set; }
		public override ExpressionType NodeType { get { return (ExpressionType)SqlExpressionType.StatementList; } }

		public SqlStatementListExpression(params Expression[] statements)
			: this(statements.ToReadOnlyList())
		{
		}

		public SqlStatementListExpression(IEnumerable<Expression> statements)
			: this(statements.ToReadOnlyList())
		{
		}
		
		public SqlStatementListExpression(IReadOnlyList<Expression> statements)
			: base(statements.Count > 0 ? statements[statements.Count - 1].Type : typeof(void))
		{
			this.Statements = statements;
		}
	}
}