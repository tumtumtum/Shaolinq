// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)

using System.Collections.Generic;

namespace Shaolinq.Persistence.Linq.Expressions
{
	public class SqlIndexExpressionBase
		: SqlBaseExpression
	{
		public string IndexName { get; }
		public IReadOnlyList<SqlIndexedColumnExpression> Columns { get; }
		public IReadOnlyList<SqlIndexedColumnExpression> IncludedColumns { get; }

		public SqlIndexExpressionBase(string indexName, IReadOnlyList<SqlIndexedColumnExpression> columns, IReadOnlyList<SqlIndexedColumnExpression> includedColumns)
			: base(typeof(void))
		{
			this.IndexName = indexName;
			this.Columns = columns;
			this.IncludedColumns = includedColumns;
		}
	}
}