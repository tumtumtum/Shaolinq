using System.Collections.Generic;
using System.Linq.Expressions;

namespace Shaolinq.Persistence.Sql.Linq.Expressions
{
	public class SqlStatementListExpression
		: SqlBaseExpression
	{
		public List<Expression> Statements { get; set; }

		public override ExpressionType NodeType
		{
			get
			{
				return (ExpressionType)SqlExpressionType.StatementList;
			}
		}

		public SqlStatementListExpression(List<Expression> statements)
			: base(typeof(void))
		{
			Statements = statements;
		}
	}
}
