using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Shaolinq.Persistence.Linq;
using Shaolinq.Persistence.Linq.Expressions;

namespace Shaolinq.MySql
{
	public class MySqlNestedTableReferenceInUpdateFixer
		: SqlExpressionVisitor
	{
		private bool inUpdate;
		private string updateTableName;

		public static Expression Fix(Expression expression)
		{
			return new MySqlNestedTableReferenceInUpdateFixer().Visit(expression);
		}

		protected override Expression VisitUpdate(SqlUpdateExpression expression)
		{
			try
			{
				var newSource = this.Visit(expression.Source);

				this.inUpdate = true;

				updateTableName = (expression.Source as SqlTableExpression).Name;

				var newWhere = this.Visit(expression.Where);
				var newAssignments = this.VisitExpressionList(expression.Assignments);

				if (newSource != expression.Source || newWhere != expression.Where || newAssignments != expression.Assignments)
				{
					return new SqlUpdateExpression(newSource, newAssignments, newWhere);
				}

				return expression;
			}
			finally
			{
				this.inUpdate = false;
			}
		}

		protected override Expression VisitTable(SqlTableExpression table)
		{
			if (table.Name != updateTableName || !inUpdate)
			{
				return base.VisitTable(table);
			}

			return new SqlSelectExpression(table.Type, table.Alias, new SqlColumnDeclaration[0], table.ChangeAlias(null), null, null);
		}
	}
}
