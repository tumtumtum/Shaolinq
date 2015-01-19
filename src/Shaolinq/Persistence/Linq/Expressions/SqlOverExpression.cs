using System.Linq.Expressions;
using Platform.Collections;

namespace Shaolinq.Persistence.Linq.Expressions
{
	public class SqlOverExpression
		: SqlBaseExpression
	{
		public Expression Source { get; private set; }
		public IReadOnlyList<Expression> OrderBy { get; private set; }
		public override ExpressionType NodeType { get { return (ExpressionType)SqlExpressionType.Over; } }


		public SqlOverExpression(Expression source, IReadOnlyList<Expression> orderBy)
			: base(typeof(void))
		{
			this.Source = source;
			this.OrderBy = orderBy;
		}
	}
}
