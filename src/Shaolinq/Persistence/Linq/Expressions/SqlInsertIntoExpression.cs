// Copyright (c) 2007-2014 Thong Nguyen (tumtumtum@gmail.com)

using System.Collections.Generic;
using System.Linq.Expressions;
using Platform.Collections;

namespace Shaolinq.Persistence.Linq.Expressions
{
	public class SqlInsertIntoExpression
		: SqlBaseExpression
	{
		public SqlTableExpression Table { get; private set; }
		public IReadOnlyList<string> ColumnNames { get; private set; }
		public IReadOnlyList<Expression> ValueExpressions { get; private set; }
		public IReadOnlyList<string> ReturningAutoIncrementColumnNames { get; private set; }
		public override ExpressionType NodeType { get { return (ExpressionType)SqlExpressionType.InsertInto; } }

		public SqlInsertIntoExpression(SqlTableExpression table, IEnumerable<string> columnNames, IEnumerable<string> returningAutoIncrementColumnNames, IEnumerable<Expression> valueExpressions)
			: this(table, columnNames.ToReadOnlyList(), returningAutoIncrementColumnNames.ToReadOnlyList(), valueExpressions.ToReadOnlyList())
		{	
		}

		public SqlInsertIntoExpression(SqlTableExpression table, IReadOnlyList<string> columnNames, IReadOnlyList<string> returningAutoIncrementColumnNames, IReadOnlyList<Expression> valueExpressions)
			: base(typeof(void))
		{
			this.Table = table;
			this.ColumnNames = columnNames;
			this.ReturningAutoIncrementColumnNames = returningAutoIncrementColumnNames;
			this.ValueExpressions = valueExpressions;
		}
	}
}
