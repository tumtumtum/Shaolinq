// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Expressions
{
	/// <summary>
	/// A SQL aggregate expression such as MAX(columnn) or COUNT(*) or COUNT(DISTINCT column)
	/// </summary>
	public class SqlAggregateExpression
		: SqlBaseExpression
	{
		public bool IsDistinct { get; }
		public Expression Argument { get; }
		public SqlAggregateType AggregateType { get; }
		public override ExpressionType NodeType => (ExpressionType)SqlExpressionType.Aggregate;

		public SqlAggregateExpression(Type type, SqlAggregateType aggType, Expression argument, bool isDistinct)
			: base(type)
		{
			this.AggregateType = aggType;
			this.Argument = argument;
			this.IsDistinct = isDistinct;
		}

		public Expression ChangeArgument(Expression argument)
		{
			return new SqlAggregateExpression(this.Type, this.AggregateType, argument, this.IsDistinct);
		}
	}
}
