// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)

using System.Collections.Generic;
using System.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Expressions
{
	public class SqlColumnDefinitionExpression
		: SqlBaseExpression
	{
		public string ColumnName { get; }
		public Expression ColumnType { get; }
		public IReadOnlyList<SqlConstraintExpression> ConstraintExpressions { get; }
		public override ExpressionType NodeType => (ExpressionType)SqlExpressionType.ColumnDefinition;

		public SqlColumnDefinitionExpression(string columnName, Expression columnTypeName, IReadOnlyList<SqlConstraintExpression> constraintExpressions)
			: base(typeof(void))
		{
			this.ColumnName = columnName;
			this.ColumnType = columnTypeName;
			this.ConstraintExpressions = constraintExpressions;
		}

		public SqlColumnDefinitionExpression ChangeConstraints(IReadOnlyList<SqlConstraintExpression> newConstraints)
		{
			return new SqlColumnDefinitionExpression(this.ColumnName, this.ColumnType, newConstraints);
		}
	}
}
