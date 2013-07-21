using System;
using System.Linq.Expressions;

namespace Shaolinq.Persistence.Sql.Linq.Expressions
{
	/// <summary>
	/// A SQL aggregate expression such as MAX(columnn) or COUNT(*) or COUNT(DISTINCT column)
	/// </summary>
	public class SqlAggregateExpression
		: SqlBaseExpression
	{
		public bool IsDistinct { get; private set; }
		public Expression Argument { get; private set; }
		public SqlAggregateType AggregateType { get; private set; }

		public override ExpressionType NodeType
		{
			get
			{
				return (ExpressionType)SqlExpressionType.Aggregate;
			}
		}

		public SqlAggregateExpression(Type type, SqlAggregateType aggType, Expression argument, bool isDistinct)
			: base(type)
		{
			this.AggregateType = aggType;
			this.Argument = argument;
			this.IsDistinct = isDistinct;
		}
	}
}
