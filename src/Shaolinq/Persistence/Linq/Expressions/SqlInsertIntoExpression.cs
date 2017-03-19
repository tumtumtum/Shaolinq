// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System.Collections.Generic;
using System.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Expressions
{
	public class SqlInsertIntoExpression
		: SqlBaseExpression
	{
		public Expression Source { get; }
		public Expression WithExpression { get; }
		public bool RequiresIdentityInsert { get; }
		public IReadOnlyList<string> ColumnNames { get; }
		public IReadOnlyList<Expression> ValueExpressions { get; }
		public Expression ValuesExpression { get; }
		public IReadOnlyList<string> ReturningAutoIncrementColumnNames { get; }
		public override ExpressionType NodeType => (ExpressionType)SqlExpressionType.InsertInto;

		public SqlInsertIntoExpression(Expression source, IEnumerable<string> columnNames, IEnumerable<string> returningAutoIncrementColumnNames, IEnumerable<Expression> valueExpressions)
			: this(source, columnNames.ToReadOnlyCollection(), returningAutoIncrementColumnNames.ToReadOnlyCollection(), valueExpressions.ToReadOnlyCollection())
		{	
		}

		public SqlInsertIntoExpression(Expression source, IReadOnlyList<string> columnNames, IReadOnlyList<string> returningAutoIncrementColumnNames, IReadOnlyList<Expression> valueExpressions)
			: this(source, columnNames, returningAutoIncrementColumnNames, valueExpressions, null, false)
		{	
		}

		public SqlInsertIntoExpression(Expression source, IReadOnlyList<string> columnNames, IReadOnlyList<string> returningAutoIncrementColumnNames, IReadOnlyList<Expression> valueExpressions, Expression withExpression, bool requiresIdentityInsert)
			: base(typeof(void))
		{
			this.Source = source;
			this.ColumnNames = columnNames;
			this.ReturningAutoIncrementColumnNames = returningAutoIncrementColumnNames;
			this.ValueExpressions = valueExpressions;
			this.WithExpression = withExpression;
			this.RequiresIdentityInsert = requiresIdentityInsert;
		}

		public SqlInsertIntoExpression(Expression source, IReadOnlyList<string> columnNames, IReadOnlyList<string> returningAutoIncrementColumnNames, Expression valuesExpression, Expression withExpression, bool requiresIdentityInsert)
			: base(typeof(void))
		{
			this.Source = source;
			this.ColumnNames = columnNames;
			this.ReturningAutoIncrementColumnNames = returningAutoIncrementColumnNames;
			this.ValuesExpression = valuesExpression;
			this.WithExpression = withExpression;
			this.RequiresIdentityInsert = requiresIdentityInsert;
		}

		public SqlInsertIntoExpression ChangeSourceAndValueExpressions(SqlProjectionExpression source, IReadOnlyList<Expression> valueExpressions)
		{
			return new SqlInsertIntoExpression(source, this.ColumnNames, this.ReturningAutoIncrementColumnNames, valueExpressions, this.WithExpression, this.RequiresIdentityInsert);
		}

		public SqlInsertIntoExpression ChangeColumnNamesAndValues(IReadOnlyList<string> columnNames, IReadOnlyList<Expression> valueExpressions)
		{
			return new SqlInsertIntoExpression(this.Source, columnNames, this.ReturningAutoIncrementColumnNames, valueExpressions, this.WithExpression, this.RequiresIdentityInsert);
		}
	}
}
