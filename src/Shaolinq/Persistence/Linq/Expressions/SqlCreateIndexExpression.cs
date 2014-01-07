// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

using System.Linq.Expressions;
using System.Collections.ObjectModel;

namespace Shaolinq.Persistence.Linq.Expressions
{
	public class SqlCreateIndexExpression
		: SqlBaseExpression
	{
		public string IndexName { get; set; }
		public SqlTableExpression Table { get; set; }
		public bool Unique { get; set; }
		public bool LowercaseIndex { get; set; }
		public IndexType IndexType { get; set; }
		public ReadOnlyCollection<SqlColumnExpression> Columns { get; set; }

		public override ExpressionType NodeType
		{
			get
			{
				return (ExpressionType)SqlExpressionType.CreateIndex;
			}
		}

		public SqlCreateIndexExpression(string indexName, SqlTableExpression table, bool unique, bool lowercaseIndex, IndexType indexType, ReadOnlyCollection<SqlColumnExpression> columns)
			: base(typeof(void))
		{
			this.IndexName = indexName;
			this.Table = table;
			this.Unique = unique;
			this.LowercaseIndex = lowercaseIndex;
			this.IndexType = indexType;
			this.Columns = columns;
		}
	}
}
