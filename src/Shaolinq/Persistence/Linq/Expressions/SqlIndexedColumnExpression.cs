using System.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Expressions
{
	public class SqlIndexedColumnExpression
		: SqlBaseExpression
	{
		public SortOrder SortOrder { get; private set; }
		public SqlColumnExpression Column { get; private set; }
		
		public override ExpressionType NodeType
		{
			get
			{
				return (ExpressionType)SqlExpressionType.IndexedColumn;
			}
		}

		public SqlIndexedColumnExpression(SqlColumnExpression column, SortOrder sortOrder)
			: base(typeof(void))
		{
			this.Column = column;
			this.SortOrder = sortOrder;
		}
	}
}
