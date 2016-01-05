using System;
using System.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Expressions
{
	public class SqlQueryArgumentExpression
		: SqlBaseExpression
	{
		public int Index { get; }
		public override ExpressionType NodeType => (ExpressionType)SqlExpressionType.QueryArgument;

		public SqlQueryArgumentExpression(Type type, int index)
			: base(type)
		{
			this.Index = index;
		}
	}
}
