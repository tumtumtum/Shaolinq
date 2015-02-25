// Copyright (c) 2007-2014 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Linq;
using System.Collections.Generic;
using System.Linq.Expressions;
using Shaolinq.Persistence.Linq.Expressions;

namespace Shaolinq.Persistence.Linq
{
	public class Nominator
		: SqlExpressionVisitor
	{
		public readonly HashSet<Expression> candidates;
		protected readonly Func<Expression, bool> fnCanBeColumn;

		public Nominator(Func<Expression, bool> canBeColumn)
		{
			this.fnCanBeColumn = canBeColumn;
			candidates = new HashSet<Expression>();
		}

		public virtual HashSet<Expression> Nominate(Expression expression)
		{
			this.Visit(expression);

			return this.candidates;
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
	public class IncludeWhereClauseNominator
		: Nominator
	{
		public IncludeWhereClauseNominator(Func<Expression, bool> canBeColumn)
			: base(canBeColumn)
		{
		}
	}

	public class NormalNominator
		: IncludeWhereClauseNominator
	{
		public NormalNominator(Func<Expression, bool> canBeColumn)
			: base(canBeColumn)
		{
		}

		protected override Expression VisitJoin(SqlJoinExpression join)
		{
			var left = this.Visit(join.Left);
			var right = this.Visit(join.Right);

			var condition = join.JoinCondition;

			if (left != join.Left || right != join.Right || condition != join.JoinCondition)
			{
				return new SqlJoinExpression(join.Type, join.JoinType, left, right, condition);
			}

			return join;
		}

		protected override Expression VisitSelect(SqlSelectExpression selectExpression)
		{
			var from = VisitSource(selectExpression.From);

			var orderBy = selectExpression.OrderBy;
			var groupBy = selectExpression.GroupBy;
			var skip = selectExpression.Skip;
			var take = selectExpression.Take;
			var columns = VisitColumnDeclarations(selectExpression.Columns);

			if (from != selectExpression.From || columns != selectExpression.Columns || orderBy != selectExpression.OrderBy || groupBy != selectExpression.GroupBy || take != selectExpression.Take || skip != selectExpression.Skip)
			{
				return new SqlSelectExpression(selectExpression.Type, selectExpression.Alias, columns, from, selectExpression.Where, orderBy, groupBy, selectExpression.Distinct, skip, take, selectExpression.ForUpdate);
			}

			return selectExpression;
		}
	}

	public class OnlyWhereClauseNominator
		: IncludeWhereClauseNominator
	{
		private bool inWhereClause;

		public OnlyWhereClauseNominator(Func<Expression, bool> canBeColumn)
			: base(canBeColumn)
		{
		}

		protected override Expression VisitSelect(SqlSelectExpression selectExpression)
		{
			this.VisitSource(selectExpression.From);
			this.VisitColumnDeclarations(selectExpression.Columns);

			var localInWhereClause = this.inWhereClause;

			this.inWhereClause = true;

			this.Visit(selectExpression.Where);
			this.inWhereClause = localInWhereClause;

			return selectExpression;
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
					if (this.inWhereClause)
					{
						candidates.Add(expression);
					}
				}
			}

			return expression;
		}
	}

	public class ColumnProjector
		: SqlExpressionVisitor
	{
		

		private int columnIndex; 
		private readonly string newAlias;
		private readonly string[] existingAliases;

		private readonly HashSet<string> columnNames; 
		private readonly HashSet<Expression> candidates;
		private readonly List<SqlColumnDeclaration> columns;
		private readonly TypeDescriptorProvider typeDescriptorProvider;
		private readonly Nominator nominator;
		private readonly Dictionary<SqlColumnExpression, SqlColumnExpression> mappedColumnExpressions;

		internal ColumnProjector(TypeDescriptorProvider typeDescriptorProvider, Nominator nominator, Expression expression, string newAlias, params string[] existingAliases)
		{
			columnNames = new HashSet<string>();
			columns = new List<SqlColumnDeclaration>();
			mappedColumnExpressions = new Dictionary<SqlColumnExpression, SqlColumnExpression>();

			this.typeDescriptorProvider = typeDescriptorProvider;
			this.nominator = nominator;
			this.newAlias = newAlias;
			this.existingAliases = existingAliases;
			this.candidates = nominator.Nominate(expression);
		}

		public static ProjectedColumns ProjectColumns(TypeDescriptorProvider typeDescriptorProvider, Nominator nominator, Expression expression, string newAlias, params string[] existingAliases)
		{
			var projector = new ColumnProjector(typeDescriptorProvider, nominator, expression, newAlias, existingAliases);

			expression = projector.Visit(expression);

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
