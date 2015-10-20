// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Expressions
{
	public class SqlSimpleConstraintExpression
		: SqlBaseExpression
	{
		public object Value { get; }
		public string[] ColumnNames { get; }
		public SqlSimpleConstraint Constraint { get; }
		public override ExpressionType NodeType => (ExpressionType)SqlExpressionType.SimpleConstraint;

		public SqlSimpleConstraintExpression(SqlSimpleConstraint constraint, string[] columnNames = null, object value = null)
			: base(typeof(void))
		{
			this.Constraint = constraint;
			this.ColumnNames = columnNames;
			this.Value = value;
		}
	}
}