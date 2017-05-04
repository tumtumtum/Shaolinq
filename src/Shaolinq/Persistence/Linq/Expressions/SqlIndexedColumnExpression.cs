// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Expressions
{
	public class SqlIndexedColumnExpression
		: SqlBaseExpression
	{
		public bool LowercaseIndex { get; }
		public bool IncludeOnly { get; }
		public SortOrder SortOrder { get; }
		public Expression Column { get; }
		public override ExpressionType NodeType => (ExpressionType)SqlExpressionType.IndexedColumn;

		public SqlIndexedColumnExpression(Expression column, SortOrder sortOrder = default(SortOrder), bool lowercaseIndex = default(bool), bool includeOnly = false)
			: base(typeof(void))
		{
			this.Column = column;
			this.SortOrder = sortOrder;
			this.LowercaseIndex = lowercaseIndex;
			this.IncludeOnly = includeOnly;
		}
	}
}
