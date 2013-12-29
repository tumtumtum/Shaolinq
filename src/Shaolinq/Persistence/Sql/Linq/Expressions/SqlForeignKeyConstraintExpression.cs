using System.Collections.ObjectModel;
using System.Linq.Expressions;

namespace Shaolinq.Persistence.Sql.Linq.Expressions
{
	public class SqlForeignKeyConstraintExpression
		: SqlBaseExpression
	{
		public string ConstraintName { get; set; }
		public ReadOnlyCollection<string> ColumnNames { get; set; }
		public SqlReferencesColumnExpression ReferencesColumnExpression { get; private set; }

		public override ExpressionType NodeType
		{
			get
			{
				return (ExpressionType)SqlExpressionType.ForeignKeyConstraint;
			}
		}

		public SqlForeignKeyConstraintExpression(string constraintName, ReadOnlyCollection<string> columnNames, SqlReferencesColumnExpression referencesColumnExpression)
			: base(typeof(void))
		{
			this.ConstraintName = constraintName;
			this.ColumnNames = columnNames;
			this.ReferencesColumnExpression = referencesColumnExpression;
		}
	}
}
