// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System.Collections.Generic;
using System.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Expressions
{
	public class SqlColumnDefinitionExpression
		: SqlBaseExpression
	{
		public string ColumnName { get; }
		public Expression ColumnType { get; }
		public IReadOnlyList<Expression> ConstraintExpressions { get; }
		public override ExpressionType NodeType => (ExpressionType)SqlExpressionType.ColumnDefinition;

		public SqlColumnDefinitionExpression(string columnName, Expression columnTypeName, IEnumerable<Expression> constraintExpressions)
			: this(columnName, columnTypeName, constraintExpressions.ToReadOnlyCollection())
		{	
		}

		public SqlColumnDefinitionExpression(string columnName, Expression columnTypeName, IReadOnlyList<Expression> constraintExpressions)
			: base(typeof(void))
		{
			this.ColumnName = columnName;
			this.ColumnType = columnTypeName;
			this.ConstraintExpressions = constraintExpressions;
		}

		public SqlColumnDefinitionExpression UpdateConstraints(IEnumerable<Expression> newConstraints)
		{
			return new SqlColumnDefinitionExpression(this.ColumnName, this.ColumnType, newConstraints);
		}
	}
}
