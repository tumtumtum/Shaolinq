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
			private bool isBlocked;
			private readonly HashSet<Expression> candidates;
			private readonly Func<Expression, bool> fnCanBeColumn;

			private Nominator(Func<Expression, bool> canBeColumn)
			{
				this.fnCanBeColumn = canBeColumn;
				candidates = new HashSet<Expression>();
				isBlocked = false;
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
					var saveIsBlocked = isBlocked;

					isBlocked = false;

					if (expression.NodeType != (ExpressionType)SqlExpressionType.Subquery)
					{
						base.Visit(expression);
					}

					if (!isBlocked)
					{
						if (fnCanBeColumn(expression))
						{
							candidates.Add(expression);
						}
						else
						{
							isBlocked = true;
						}
					}

					isBlocked |= saveIsBlocked;
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
		private readonly Dictionary<MemberInitExpression, SqlObjectReferenceExpression> sqlObjectReferenceByMemberInit;
		private readonly Dictionary<SqlColumnExpression, SqlColumnExpression> mappedColumnExpressions;

		internal ColumnProjector(Func<Expression, bool> canBeColumn, Expression expression, string newAlias, Dictionary<MemberInitExpression, SqlObjectReferenceExpression> sqlObjectReferenceByMemberInit, params string[] existingAliases)
		{
			columnNames = new HashSet<string>();
			columns = new List<SqlColumnDeclaration>();
			mappedColumnExpressions = new Dictionary<SqlColumnExpression, SqlColumnExpression>();

			this.sqlObjectReferenceByMemberInit = sqlObjectReferenceByMemberInit;
			this.newAlias = newAlias;
			this.existingAliases = existingAliases;
			this.candidates = Nominator.Nominate(canBeColumn, expression);
		}

		public static ProjectedColumns ProjectColumns(Func<Expression, bool> canBeColumn, Expression expression, string newAlias, Dictionary<MemberInitExpression, SqlObjectReferenceExpression> sqlObjectReferenceByMemberInit, params string[] existingAliases)
		{
			var projector = new ColumnProjector(canBeColumn, expression, newAlias, sqlObjectReferenceByMemberInit, existingAliases);

			expression = projector.Visit(expression);

			return new ProjectedColumns(expression, projector.columns.AsReadOnly());
		}

		protected override Expression VisitMemberInit(MemberInitExpression expression)
		{
			var retval = base.VisitMemberInit(expression);
			var newMemberInit = retval as MemberInitExpression;

			if (retval != expression && newMemberInit != null)
			{
				this.sqlObjectReferenceByMemberInit[expression] = new SqlObjectReferenceExpression(newMemberInit.Type, newMemberInit.Bindings);

				return retval;
			}

			return retval;
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
				return base.Visit(expression);
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
				name = baseName + (suffix++);
			}

			return name;
		}
        
		private string GetNextColumnName()
		{
			return GetUniqueColumnName("COL" + (columnIndex++));
		}
	}
}
