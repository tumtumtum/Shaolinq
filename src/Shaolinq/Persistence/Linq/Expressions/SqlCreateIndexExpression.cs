// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Expressions
{
	public class SqlCreateIndexExpression
		: SqlIndexExpressionBase
	{
		public bool Unique { get; }
		public bool IfNotExist { get; }
		public bool LowercaseIndex { get; }
		public IndexType IndexType { get; }
		public SqlTableExpression Table { get; }
		public Expression Where { get; }
		public bool? Clustered { get; }
		public override ExpressionType NodeType => (ExpressionType)SqlExpressionType.CreateIndex;

		public SqlCreateIndexExpression(string indexName, SqlTableExpression table, bool unique, bool lowercaseIndex, IndexType indexType, bool ifNotExist, IEnumerable<SqlIndexedColumnExpression> columns, IEnumerable<SqlColumnExpression> includedColumns)
			: this(indexName, table, unique, lowercaseIndex, indexType, ifNotExist, columns.ToReadOnlyCollection(), includedColumns.ToReadOnlyCollection())
		{
		}

		public SqlCreateIndexExpression(string indexName, SqlTableExpression table, bool unique, bool lowercaseIndex, IndexType indexType, bool ifNotExist, IReadOnlyList<SqlIndexedColumnExpression> columns, IReadOnlyList<SqlColumnExpression>  includedColumns, Expression where = null, bool? clustered =  null)
			: base(indexName, columns, includedColumns)
		{
			this.Table = table;
			this.Unique = unique;
			this.LowercaseIndex = lowercaseIndex;
			this.IndexType = indexType;
			this.IfNotExist = ifNotExist;
			this.Where = where;
			this.Clustered = clustered;
		}

		public SqlCreateIndexExpression ChangeWhere(Expression where)
		{
			return new SqlCreateIndexExpression(this.IndexName, this.Table, this.Unique, this.LowercaseIndex, this.IndexType, this.IfNotExist, this.Columns, this.IncludedColumns, where);
		}
	}
}
