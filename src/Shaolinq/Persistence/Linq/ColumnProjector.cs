// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Shaolinq.Persistence.Linq.Expressions;

namespace Shaolinq.Persistence.Linq
{
	public class ColumnProjector
		: SqlExpressionVisitor
	{
		private int columnIndex; 
		private readonly string newAlias;
		private readonly string[] existingAliases;

		private readonly HashSet<string> columnNames; 
		private readonly HashSet<Expression> candidates;
		private readonly List<SqlColumnDeclaration> columns;
		private readonly Dictionary<SqlColumnExpression, SqlColumnExpression> mappedColumnExpressions;

		internal ColumnProjector(Nominator nominator, Expression expression, IEnumerable<SqlColumnDeclaration> existingColumns, string newAlias, IEnumerable<string> existingAliases)
		{
			this.mappedColumnExpressions = new Dictionary<SqlColumnExpression, SqlColumnExpression>();
			
			if (existingColumns != null)
			{
				this.columns = new List<SqlColumnDeclaration>(existingColumns);
				this.columnNames = new HashSet<string>(this.columns.Select(c => c.Name));
			}
			else
			{
				this.columns = new List<SqlColumnDeclaration>();
				this.columnNames = new HashSet<string>();
			}
	
			this.newAlias = newAlias;
			this.existingAliases = existingAliases.ToArray();
			this.candidates = nominator.Nominate(expression);
		}

		internal static ProjectedColumns ProjectColumns(Nominator nominator, Expression expression, IEnumerable<SqlColumnDeclaration> existingColumns, string newAlias, IEnumerable<string> existingAliases)
		{
			var projector = new ColumnProjector(nominator, expression, existingColumns, newAlias, existingAliases);

			expression = projector.Visit(expression);

			return new ProjectedColumns(expression, projector.columns.ToReadOnlyCollection());
		}
		
		private Expression ProcessExpression(Expression expression)
		{
			if (expression.NodeType == (ExpressionType)SqlExpressionType.Column)
			{
				SqlColumnExpression mappedColumnExpression;

				var column = (SqlColumnExpression)expression;

				if (this.mappedColumnExpressions.TryGetValue(column, out mappedColumnExpression))
				{
					return mappedColumnExpression;
				}

				if (this.existingAliases.Contains(column.SelectAlias))
				{
					var columnName = this.GetUniqueColumnName(column.Name);

					this.columns.Add(new SqlColumnDeclaration(columnName, column));
					mappedColumnExpression = new SqlColumnExpression(column.Type, this.newAlias, columnName);

					this.mappedColumnExpressions[column] = mappedColumnExpression;
					this.columnNames.Add(columnName);

					return mappedColumnExpression;
				}

				// Must be referring to outer scope

				return column;
			}
			else
			{
				var columnName = this.GetNextColumnName();

				this.columnNames.Add(columnName);
				this.columns.Add(new SqlColumnDeclaration(columnName, expression));

				return new SqlColumnExpression(expression.Type, this.newAlias, columnName);
			}
		}

		protected override Expression Visit(Expression expression)
		{
			if (this.candidates.Contains(expression))
			{
				if (expression.NodeType == (ExpressionType)SqlExpressionType.ObjectReference)
				{
					return base.Visit(expression);
				}
				else
				{
					return this.ProcessExpression(expression);
				}
			}
			else
			{
				return base.Visit(expression);
			}
		}

		private bool IsColumnNameInUse(string name)
		{
			return this.columnNames.Contains(name);
		}

		private string GetUniqueColumnName(string name)
		{
			var suffix = 1; 
			var baseName = name;

			while (this.IsColumnNameInUse(name))
			{
				name = baseName + "_" + (suffix++);
			}

			return name;
		}
        
		private string GetNextColumnName()
		{
			string name;

			do
			{
				name = this.GetUniqueColumnName("COL" + (this.columnIndex++));
			}
			while (this.IsColumnNameInUse(name));

			return name;
		}
	}
}
