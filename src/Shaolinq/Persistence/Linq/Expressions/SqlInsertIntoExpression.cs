// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System.Collections.Generic;
using System.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Expressions
{
	public class SqlInsertIntoExpression
		: SqlBaseExpression
	{
		public Expression Source { get; }
		public IReadOnlyList<string> ColumnNames { get; }
		public IReadOnlyList<Expression> ValueExpressions { get; }
		public IReadOnlyList<string> ReturningAutoIncrementColumnNames { get; }
		public override ExpressionType NodeType => (ExpressionType)SqlExpressionType.InsertInto;

		public SqlInsertIntoExpression(Expression source, IEnumerable<string> columnNames, IEnumerable<string> returningAutoIncrementColumnNames, IEnumerable<Expression> valueExpressions)
			: this(source, columnNames.ToReadOnlyCollection(), returningAutoIncrementColumnNames.ToReadOnlyCollection(), valueExpressions.ToReadOnlyCollection())
		{	
		}

		public SqlInsertIntoExpression(Expression source, IReadOnlyList<string> columnNames, IReadOnlyList<string> returningAutoIncrementColumnNames, IReadOnlyList<Expression> valueExpressions)
			: base(typeof(void))
		{
			this.Source = source;
			this.ColumnNames = columnNames;
			this.ReturningAutoIncrementColumnNames = returningAutoIncrementColumnNames;
			this.ValueExpressions = valueExpressions;
		}

		public SqlInsertIntoExpression ChangeSourceAndValueExpressions(SqlProjectionExpression source, IReadOnlyList<Expression> valueExpressions)
		{
			return new SqlInsertIntoExpression(source, this.ColumnNames, this.ReturningAutoIncrementColumnNames, valueExpressions);
		}
	}
}
