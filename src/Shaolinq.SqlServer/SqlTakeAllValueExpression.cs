using System.Linq.Expressions;
using Shaolinq.Persistence.Linq.Expressions;

namespace Shaolinq.SqlServer
{
	public class SqlTakeAllValueExpression
		: SqlBaseExpression
	{
		public override ExpressionType NodeType { get { return ExpressionType.Extension; } }

		public SqlTakeAllValueExpression()
			: base(typeof(int))
		{
		}
	}
}
