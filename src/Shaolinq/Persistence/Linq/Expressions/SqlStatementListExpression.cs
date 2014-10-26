// Copyright (c) 2007-2014 Thong Nguyen (tumtumtum@gmail.com)

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Expressions
{
	public class SqlStatementListExpression
		: SqlBaseExpression
	{
		public ReadOnlyCollection<Expression> Statements { get; private set; }
		public override ExpressionType NodeType { get { return (ExpressionType)SqlExpressionType.StatementList; } }

		public SqlStatementListExpression(params Expression[] statements)
			: this((IList<Expression>)statements)
		{	
		}

		public SqlStatementListExpression(IEnumerable<Expression> statements)
			: this(statements.ToList())
		{	
		}

		public SqlStatementListExpression(IList<Expression> statements)
			: this(new ReadOnlyCollection<Expression>(statements))
		{
		}

		public SqlStatementListExpression(ReadOnlyCollection<Expression> statements)
			: base(typeof(void))
		{
			this.Statements = statements;
		}
	}
}