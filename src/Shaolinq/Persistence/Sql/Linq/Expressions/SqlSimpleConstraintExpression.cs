// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

using System.Linq.Expressions;

namespace Shaolinq.Persistence.Sql.Linq.Expressions
{
	public class SqlSimpleConstraintExpression
		: SqlBaseExpression
	{
		public string ColumnName { get; private set; }
		public SqlSimpleConstraint Constrant { get; private set; }

		public override ExpressionType NodeType
		{
			get
			{
				return (ExpressionType)SqlExpressionType.SimpleConstraint;
			}
		}

		public SqlSimpleConstraintExpression(SqlSimpleConstraint constraint, string columnName = null)
			: base(typeof(void))
		{
			this.Constrant = constraint;
			this.ColumnName = columnName;
		}
	}
}
