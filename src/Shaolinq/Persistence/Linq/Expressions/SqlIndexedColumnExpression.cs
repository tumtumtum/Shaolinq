// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Expressions
{
	public class SqlIndexedColumnExpression
		: SqlBaseExpression
	{
		public bool LowercaseIndex { get; }
		public SortOrder SortOrder { get; }
		public SqlColumnExpression Column { get; }
		public override ExpressionType NodeType => (ExpressionType)SqlExpressionType.IndexedColumn;

		public SqlIndexedColumnExpression(SqlColumnExpression column, SortOrder sortOrder, bool lowercaseIndex)
			: base(typeof(void))
		{
			this.Column = column;
			this.SortOrder = sortOrder;
			this.LowercaseIndex = lowercaseIndex;
		}
	}
}
