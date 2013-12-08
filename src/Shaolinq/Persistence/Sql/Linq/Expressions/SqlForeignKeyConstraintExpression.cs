using System.Linq.Expressions;

namespace Shaolinq.Persistence.Sql.Linq.Expressions
{
	public class SqlForeignKeyConstraintExpression
		: SqlBaseExpression
	{
		public string ColumnName { get; set; }
		public SqlReferencesColumnExpression ReferencesColumnExpression { get; private set; }

		public override ExpressionType NodeType
		{
			get
			{
				return (ExpressionType)SqlExpressionType.ForeignKeyConstraint;
			}
		}

		public SqlForeignKeyConstraintExpression(string columnName, SqlReferencesColumnExpression referencesColumnExpression)
			: base(typeof(void))
		{
			this.ColumnName = columnName;
			this.ReferencesColumnExpression = referencesColumnExpression;
		}
	}
}
