using System.Collections.Generic;
using System.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Expressions
{
	public class SqlOrganizationIndexExpression
		: SqlIndexExpressionBase
	{
		public override ExpressionType NodeType => (ExpressionType)SqlExpressionType.OrganizationIndex;

		/// <summary>
		/// Creates a new <c>SqlOrganizationIndexExpression</c>
		/// </summary>
		/// <param name="columns">The columns in the index or null to remove an explicitly defined organization index</param>
		/// <param name="includedColumns">Columns to include int he organization index (default depends on underlying RDBMS)</param>
		public SqlOrganizationIndexExpression(IReadOnlyList<SqlIndexedColumnExpression> columns, IReadOnlyList<SqlColumnExpression> includedColumns)
			: base(columns, includedColumns)
		{
		}

		public SqlOrganizationIndexExpression ChangeColumns(IReadOnlyList<SqlIndexedColumnExpression> columns)
		{
			return new SqlOrganizationIndexExpression(columns, this.IncludedColumns);
		}

		public SqlOrganizationIndexExpression ChangeColumns(IReadOnlyList<SqlIndexedColumnExpression> columns, IReadOnlyList<SqlColumnExpression> includedColumns)
		{
			return new SqlOrganizationIndexExpression(columns, includedColumns);
		}
	}
}