// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Shaolinq.Persistence.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Optimizers
{
	/// <summary>
	/// Removes duplicate column declarations that refer to the same underlying column.
	/// </summary>
	public class RedundantColumnRemover
		: SqlExpressionVisitor
	{
		private readonly Dictionary<SqlColumnExpression, SqlColumnExpression> visitedColumns;

		private RedundantColumnRemover()
		{
			this.visitedColumns = new Dictionary<SqlColumnExpression, SqlColumnExpression>();
		}

		public static Expression Remove(Expression expression)
		{
			return new RedundantColumnRemover().Visit(expression);
		}

		protected override Expression VisitColumn(SqlColumnExpression column)
		{
			SqlColumnExpression existing;

			if (this.visitedColumns.TryGetValue(column, out existing))
			{
				return existing;
			}

			return column;
		}

		protected override Expression VisitSelect(SqlSelectExpression select)
		{
			var columnRemoved = false; 
			
			select = (SqlSelectExpression)base.VisitSelect(select);
            
			var columnsOrderedByName = select.Columns.OrderBy(c => c.Name).ToList();

			var removedColumns = new BitArray(select.Columns.Count);
			
			for (int i = 0, n = columnsOrderedByName.Count; i < n - 1; i++)
			{
				var icolumn = columnsOrderedByName[i];
				var iNewColumnExpression = new SqlColumnExpression(icolumn.Expression.Type, select.Alias, icolumn.Name);
                
				for (var j = i + 1; j < n; j++)
				{
					if (!removedColumns.Get(j))
					{
						var jcolumn = columnsOrderedByName[j];

						if (IsSameExpression(icolumn.Expression, jcolumn.Expression))
						{
							// 'j' references should now be a reference to 'i'

							var jNewColumnExpression = new SqlColumnExpression(jcolumn.Expression.Type, select.Alias, jcolumn.Name);
							this.visitedColumns.Add(jNewColumnExpression, iNewColumnExpression);

							removedColumns.Set(j, true);
							columnRemoved = true;
						}
					}
				}
			}

			if (columnRemoved)
			{
				var newColumnDeclarations = new List<SqlColumnDeclaration>();

				for (int i = 0, n = columnsOrderedByName.Count; i < n; i++)
				{
					if (!removedColumns.Get(i))
					{
						newColumnDeclarations.Add(columnsOrderedByName[i]);
					}
				}

				select = select.ChangeColumns(newColumnDeclarations);
			}

			return select;
		}

		protected static bool IsSameExpression(Expression left, Expression right)
		{
			if (left == right)
			{
				return true;
			}

			var typedLeft = left as SqlColumnExpression;
			var typedRight = right as SqlColumnExpression;

			var retval = typedLeft != null && typedRight != null
					&& typedLeft.Type == typedRight.Type
			       && typedLeft.SelectAlias == typedRight.SelectAlias
			       && typedLeft.Name == typedRight.Name;

			return retval;
		}
	}
}
