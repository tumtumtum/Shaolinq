using System.Collections.ObjectModel;
using System.Linq.Expressions;
using Shaolinq.Persistence.Sql.Linq.Expressions;

namespace Shaolinq.Persistence.Sql.Linq
{
	public class ProjectedColumns
	{
		public Expression Projector { get; private set; }
		public ReadOnlyCollection<SqlColumnDeclaration> Columns { get; private set; }

		public ProjectedColumns(Expression projector, ReadOnlyCollection<SqlColumnDeclaration> columns)
		{
			this.Columns = columns;
			this.Projector = projector;
		}
	}
}
