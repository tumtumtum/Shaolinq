// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System.Collections.Generic;
using System.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Expressions
{
	public class SqlInsertIntoExpression
		: SqlBaseExpression
	{
		public SqlTableExpression Table { get; }
		public IReadOnlyList<string> ColumnNames { get; }
		public IReadOnlyList<Expression> ValueExpressions { get; }
		public IReadOnlyList<string> ReturningAutoIncrementColumnNames { get; }
		public override ExpressionType NodeType => (ExpressionType)SqlExpressionType.InsertInto;

		public SqlInsertIntoExpression(SqlTableExpression table, IEnumerable<string> columnNames, IEnumerable<string> returningAutoIncrementColumnNames, IEnumerable<Expression> valueExpressions)
			: this(table, columnNames.ToReadOnlyCollection(), returningAutoIncrementColumnNames.ToReadOnlyCollection(), valueExpressions.ToReadOnlyCollection())
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

		public SqlInsertIntoExpression ChangeTableAndValueExpressions(SqlTableExpression tableExpression, IReadOnlyList<Expression> valueExpressions)
		{
			return new SqlInsertIntoExpression(tableExpression, this.ColumnNames, this.ReturningAutoIncrementColumnNames, valueExpressions);
		}
	}
}
