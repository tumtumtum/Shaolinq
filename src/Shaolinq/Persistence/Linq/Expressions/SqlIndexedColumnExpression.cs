using System.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Expressions
{
	public class SqlIndexedColumnExpression
		: SqlBaseExpression
	{
		public bool Ascending { get; set; }
		public SqlColumnExpression Column { get; set; }
		
		public override ExpressionType NodeType
		{
			get
			{
				return (ExpressionType)SqlExpressionType.IndexedColumnExpression;
			}
		}

		public SqlIndexedColumnExpression(SqlColumnExpression column, bool ascending)
			: base(typeof(void))
		{
			this.Column = column;
			this.Ascending = @ascending;
		}
	}
}
