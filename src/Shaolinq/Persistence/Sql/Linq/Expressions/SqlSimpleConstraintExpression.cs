// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

namespace Shaolinq.Persistence.Sql.Linq.Expressions
{
	public class SqlSimpleConstraintExpression
		: SqlBaseExpression
	{
		public string ColumnName { get; private set; }
		public SqlSimpleConstraint Constrant { get; private set; }

		public SqlSimpleConstraintExpression(SqlSimpleConstraint constraint, string columnName = null)
			: base(typeof(void))
		{
			this.Constrant = constraint;
			this.ColumnName = columnName;
		}
	}
}
