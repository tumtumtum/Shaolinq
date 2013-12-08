using System.Collections.ObjectModel;
using System.Linq.Expressions;

namespace Shaolinq.Persistence.Sql.Linq.Expressions
{
	public class SqlReferencesColumnExpression
		: SqlBaseExpression
	{
		public string ReferencedTableName {get;private set;}
		public ReadOnlyCollection<string> ReferencedColumnNames {get;private set;}
		public SqlColumnDeferability Deferability { get; private set; }

		public override ExpressionType NodeType
		{
			get
			{
				return (ExpressionType)SqlExpressionType.ReferencesColumn;
			}
		}

		public SqlReferencesColumnExpression(string referencedTableName, SqlColumnDeferability deferability, string[] referencedColumnNames)
			: base(typeof(void))
		{
			this.ReferencedTableName = referencedTableName;
			this.Deferability = deferability;
			this.ReferencedColumnNames = new ReadOnlyCollection<string>(referencedColumnNames);
		}
	}
}
