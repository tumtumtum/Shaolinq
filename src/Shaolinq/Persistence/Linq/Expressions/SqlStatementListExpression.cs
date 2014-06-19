// Copyright (c) 2007-2014 Thong Nguyen (tumtumtum@gmail.com)

using System.Collections.ObjectModel;
using System.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Expressions
{
	public class SqlStatementListExpression
		: SqlBaseExpression
	{
		public ReadOnlyCollection<Expression> Statements { get; set; }

		public override ExpressionType NodeType
		{
			get
			{
				return (ExpressionType)SqlExpressionType.StatementList;
			}
		}

		public SqlStatementListExpression(ReadOnlyCollection<Expression> statements)
			: base(typeof(void))
		{
			this.Statements = statements;
		}
	}
}