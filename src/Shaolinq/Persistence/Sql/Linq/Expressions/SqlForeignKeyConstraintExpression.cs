namespace Shaolinq.Persistence.Sql.Linq.Expressions
{
	public class SqlForeignKeyConstraintExpression
		: SqlBaseExpression
	{
		public string ColumnName { get; set; }
		public SqlReferencesColumnExpression ReferencesColumnExpression { get; private set; }

		public SqlForeignKeyConstraintExpression(string columnName, SqlReferencesColumnExpression referencesColumnExpression)
			: base(typeof(void))
		{
			this.ColumnName = columnName;
			this.ReferencesColumnExpression = referencesColumnExpression;
		}
	}
}
