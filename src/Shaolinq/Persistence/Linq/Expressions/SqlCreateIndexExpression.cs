// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System.Collections.Generic;
using System.Linq.Expressions;
using Platform.Collections;

namespace Shaolinq.Persistence.Linq.Expressions
{
	public class SqlCreateIndexExpression
		: SqlBaseExpression
	{
		public bool Unique { get; private set; }
		public string IndexName { get; private set; }
		public bool IfNotExist { get; private set; }
		public bool LowercaseIndex { get; private set; }
		public IndexType IndexType { get; private set; }
		public SqlTableExpression Table { get; private set; }
		public IReadOnlyList<SqlIndexedColumnExpression> Columns { get; private set; }
		public override ExpressionType NodeType { get { return (ExpressionType)SqlExpressionType.CreateIndex; } }

		public SqlCreateIndexExpression(string indexName, SqlTableExpression table, bool unique, bool lowercaseIndex, IndexType indexType, bool ifNotExist, IEnumerable<SqlIndexedColumnExpression> columns)
			: this(indexName, table, unique, lowercaseIndex, indexType, ifNotExist, columns.ToReadOnlyList())
		{
		}

		public SqlCreateIndexExpression(string indexName, SqlTableExpression table, bool unique, bool lowercaseIndex, IndexType indexType, bool ifNotExist, IReadOnlyList<SqlIndexedColumnExpression> columns)
			: base(typeof(void))
		{
			this.IndexName = indexName;
			this.Table = table;
			this.Unique = unique;
			this.LowercaseIndex = lowercaseIndex;
			this.IndexType = indexType;
			this.IfNotExist = ifNotExist;
			this.Columns = columns;
		}
	}
}
