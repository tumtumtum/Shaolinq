// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System.Collections.Generic;

namespace Shaolinq.Persistence.Linq.Expressions
{
	public class SqlIndexExpressionBase
		: SqlBaseExpression
	{
		public IReadOnlyList<SqlIndexedColumnExpression> Columns { get; }
		public IReadOnlyList<SqlColumnExpression> IncludedColumns { get; }

		public SqlIndexExpressionBase(IReadOnlyList<SqlIndexedColumnExpression> columns, IReadOnlyList<SqlColumnExpression> includedColumns)
			: base(typeof(void))
		{
			this.Columns = columns;
			this.IncludedColumns = includedColumns;
		}
	}
}