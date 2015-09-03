// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Expressions
{
	public class SqlIndexedColumnExpression
		: SqlBaseExpression
	{
		public bool LowercaseIndex { get; private set; }
		public SortOrder SortOrder { get; private set; }
		public SqlColumnExpression Column { get; private set; }
		public override ExpressionType NodeType { get { return (ExpressionType)SqlExpressionType.IndexedColumn; } }

		public SqlIndexedColumnExpression(SqlColumnExpression column, SortOrder sortOrder, bool lowercaseIndex)
			: base(typeof(void))
		{
			this.Column = column;
			this.SortOrder = sortOrder;
			this.LowercaseIndex = lowercaseIndex;
		}
	}
}
