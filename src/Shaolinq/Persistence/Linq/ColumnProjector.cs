// Copyright (c) 2007-2014 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Linq;
using System.Collections.Generic;
using System.Linq.Expressions;
using Shaolinq.Persistence.Linq.Expressions;

namespace Shaolinq.Persistence.Linq
{
	public class ColumnProjector
		: SqlExpressionVisitor
	{
		private class Nominator
			: SqlExpressionVisitor
		{
			private readonly HashSet<Expression> candidates;
			private readonly Func<Expression, bool> fnCanBeColumn;

			private Nominator(Func<Expression, bool> canBeColumn)
			{
				this.fnCanBeColumn = canBeColumn;
				candidates = new HashSet<Expression>();
			}

			public static HashSet<Expression> Nominate(Func<Expression, bool> canBeColumn, Expression expression)
			{
				var nominator = new Nominator(canBeColumn);
                
				nominator.Visit(expression);

				return nominator.candidates;
			}
            
			protected override Expression Visit(Expression expression)
			{
				if (expression != null)
				{
					if (expression.NodeType != (ExpressionType)SqlExpressionType.Subquery)
					{
						base.Visit(expression);
					}

					if (fnCanBeColumn(expression))
					{
						candidates.Add(expression);
					}
				}

				return expression;
			}
		}

		private int columnIndex; 
		private readonly string newAlias;
		private readonly string[] existingAliases;

		private readonly HashSet<string> columnNames; 
		private readonly HashSet<Expression> candidates;
		private readonly List<SqlColumnDeclaration> columns;
		private readonly TypeDescriptorProvider typeDescriptorProvider;
		private readonly Dictionary<SqlColumnExpression, SqlColumnExpression> mappedColumnExpressions;

		internal ColumnProjector(TypeDescriptorProvider typeDescriptorProvider, Func<Expression, bool> canBeColumn, Expression expression, string newAlias, params string[] existingAliases)
		{
			columnNames = new HashSet<string>();
			columns = new List<SqlColumnDeclaration>();
			mappedColumnExpressions = new Dictionary<SqlColumnExpression, SqlColumnExpression>();

			this.typeDescriptorProvider = typeDescriptorProvider;
			this.newAlias = newAlias;
			this.existingAliases = existingAliases;
			this.candidates = Nominator.Nominate(canBeColumn, expression);
		}

		public static ProjectedColumns ProjectColumns(TypeDescriptorProvider typeDescriptorProvider, Func<Expression, bool> canBeColumn, Expression expression, string newAlias, params string[] existingAliases)
		{
			var projector = new ColumnProjector(typeDescriptorProvider, canBeColumn, expression, newAlias, existingAliases);

			expression = projector.Visit(expression);

			var x = projector.columns.Select(c => c.Name).Count();
			var y = projector.columns.Select(c => c.Name).Distinct().Count();

			if (x != y)
			{
				Console.WriteLine();
			}

			return new ProjectedColumns(expression, projector.columns.ToReadOnlyList());
		}

		protected override Expression VisitMemberAccess(MemberExpression memberExpression)
		{
			if (memberExpression.Member.DeclaringType != null && memberExpression.Member.DeclaringType.IsGenericType
				&& memberExpression.Member.DeclaringType.GetGenericTypeDefinition() == typeof(Nullable<>))
			{
				return this.Visit(memberExpression.Expression);
			}

			return base.VisitMemberAccess(memberExpression);
		}

		protected override Expression VisitSelect(SqlSelectExpression selectExpression)
		{
			this.VisitSource(selectExpression.From);
			this.VisitColumnDeclarations(selectExpression.Columns);

			return selectExpression;
		}
        
		private Expression ProcessExpression(Expression expression)
		{
			if (expression.NodeType == (ExpressionType)SqlExpressionType.Column)
			{
				SqlColumnExpression mappedColumnExpression;

				var column = (SqlColumnExpression)expression;

				if (mappedColumnExpressions.TryGetValue(column, out mappedColumnExpression))
				{
					return mappedColumnExpression;
				}

				if (existingAliases.Contains(column.SelectAlias))
				{
					var columnName = GetUniqueColumnName(column.Name);

					columns.Add(new SqlColumnDeclaration(columnName, column));
					mappedColumnExpression = new SqlColumnExpression(column.Type, newAlias, columnName);

					mappedColumnExpressions[column] = mappedColumnExpression;
					columnNames.Add(columnName);

					return mappedColumnExpression;
				}

				// Must be referring to outer scope

				return column;
			}
			else
			{
				var columnName = GetNextColumnName();

				columnNames.Add(columnName);
				columns.Add(new SqlColumnDeclaration(columnName, expression));

				return new SqlColumnExpression(expression.Type, newAlias, columnName);
			}
		}

		protected override Expression Visit(Expression expression)
		{
			if (candidates.Contains(expression))
			{
				if (expression.NodeType == (ExpressionType)SqlExpressionType.ObjectReference)
				{
					return base.Visit(expression);
				}
				else
				{
					return ProcessExpression(expression);
				}
			}
			else
			{
				try
				{
					return base.Visit(expression);
				}
				catch
				{
					throw;
				}
			}
		}

		private bool IsColumnNameInUse(string name)
		{
			return columnNames.Contains(name);
		}

		private string GetUniqueColumnName(string name)
		{
			var suffix = 1; 
			var baseName = name;

			while (IsColumnNameInUse(name))
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
				name = GetUniqueColumnName("COL" + (columnIndex++));
			}
			while (IsColumnNameInUse(name));

			return name;
		}
	}
}
