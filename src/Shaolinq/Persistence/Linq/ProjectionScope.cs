using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Shaolinq.Persistence.Linq.Expressions;

namespace Shaolinq.Persistence.Linq
{
	public class ProjectionScope
	{
		public Expression Reader { get; }
		public string SelectAlias { get; }
		public ProjectionScope OuterScope { get; }
		private readonly Dictionary<string, int> ordinalsByColumnName;

		public ProjectionScope(ProjectionScope outerScope, Expression reader, string selectAlias, IEnumerable<SqlColumnDeclaration> columns)
		{
			this.OuterScope = outerScope;
			this.Reader = reader;
			this.SelectAlias = selectAlias;
			this.ordinalsByColumnName = columns.Select((c, i) => new { c.Name, i }).ToDictionary(c => c.Name, c => c.i);
        }

		public bool TryGetOrdinal(SqlColumnExpression columnExpression, out Expression reader, out int ordinal)
		{
			for (var scope = this; scope != null; scope = scope.OuterScope)
			{
				if (columnExpression.SelectAlias == scope.SelectAlias 
					&& scope.ordinalsByColumnName.TryGetValue(columnExpression.Name, out ordinal))
				{
					reader = scope.Reader;

					return true;
				}
			}

			reader = null;
			ordinal = 0;

			return false;
		}
	}
}
