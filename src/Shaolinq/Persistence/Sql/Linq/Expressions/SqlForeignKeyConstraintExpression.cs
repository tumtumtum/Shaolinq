using System.Linq.Expressions;

namespace Shaolinq.Persistence.Sql.Linq.Expressions
{
	public class SqlForeignKeyConstraintExpression
		: SqlBaseExpression
	{
		public string[] ColumnNames { get; set; }
		public SqlReferencesColumnExpression ReferencesColumnExpression { get; private set; }

		public override ExpressionType NodeType
		{
			get
			{
				return (ExpressionType)SqlExpressionType.ForeignKeyConstraint;
			}
		}

		public SqlForeignKeyConstraintExpression(string[] columnNames, SqlReferencesColumnExpression referencesColumnExpression)
			: base(typeof(void))
		{
			this.ColumnNames = columnNames;
			this.ReferencesColumnExpression = referencesColumnExpression;
		}
	}
}
