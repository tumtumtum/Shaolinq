// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System.Collections.Generic;
using System.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Expressions
{
	public class SqlCreateIndexExpression
		: SqlBaseExpression
	{
		public bool Unique { get; }
		public string IndexName { get; }
		public bool IfNotExist { get; }
		public bool LowercaseIndex { get; }
		public IndexType IndexType { get; }
		public SqlTableExpression Table { get; }
        public Expression Where { get; }
		public IReadOnlyList<SqlIndexedColumnExpression> Columns { get; }
		public override ExpressionType NodeType => (ExpressionType)SqlExpressionType.CreateIndex;

		public SqlCreateIndexExpression(string indexName, SqlTableExpression table, bool unique, bool lowercaseIndex, IndexType indexType, bool ifNotExist, IEnumerable<SqlIndexedColumnExpression> columns, Expression where = null)
			: this(indexName, table, unique, lowercaseIndex, indexType, ifNotExist, columns.ToReadOnlyCollection(), where)
		{
		}

		public SqlCreateIndexExpression(string indexName, SqlTableExpression table, bool unique, bool lowercaseIndex, IndexType indexType, bool ifNotExist, IReadOnlyList<SqlIndexedColumnExpression> columns, Expression where = null)
			: base(typeof(void))
		{
			this.IndexName = indexName;
			this.Table = table;
			this.Unique = unique;
			this.LowercaseIndex = lowercaseIndex;
			this.IndexType = indexType;
			this.IfNotExist = ifNotExist;
			this.Columns = columns;
            this.Where = where;
		}

        public Expression ChangeWhere(Expression where)
        {
            return new SqlCreateIndexExpression(this.IndexName, this.Table, this.Unique, this.LowercaseIndex, this.IndexType, this.IfNotExist, this.Columns, where);
        }
	}
}
