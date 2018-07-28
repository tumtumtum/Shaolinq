// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)

using System.Collections.Generic;
using System.Linq.Expressions;
using Shaolinq.Persistence.Linq.Expressions;

namespace Shaolinq.Persistence.Linq
{
	public class SqlForeignKeyConstraintToAlterAmender
		: SqlExpressionVisitor
	{
		private bool foundStatementList = false;
		private SqlCreateTableExpression currentTable;
		private readonly List<Expression> amendments = new List<Expression>();

		public static Expression Amend(Expression expression)
		{
			var retval = new SqlForeignKeyConstraintToAlterAmender().Visit(expression);

			return retval;
		}

		protected override Expression VisitConstraint(SqlConstraintExpression expression)
		{
			if (expression.ReferencesExpression == null)
			{
				return expression;
			}

			var action = new SqlConstraintActionExpression(SqlConstraintActionType.Add, expression);
			var amendmentEpression = new SqlAlterTableExpression(this.currentTable.Table, action);

			this.amendments.Add(amendmentEpression);

			return null;
		}

		protected override Expression VisitCreateTable(SqlCreateTableExpression createTableExpression)
		{
			this.currentTable = createTableExpression;

			var retval = base.VisitCreateTable(createTableExpression);

			this.currentTable = null;

			return retval;
		}

		protected override Expression VisitStatementList(SqlStatementListExpression statementListExpression)
		{
			var localFoundStatementList = this.foundStatementList;

			this.foundStatementList = true;

			var retval = (SqlStatementListExpression)base.VisitStatementList(statementListExpression);

			if (!localFoundStatementList)
			{
				if (this.amendments.Count > 0)
				{
					var newList = new List<Expression>(retval.Statements);
					
					newList.AddRange(this.amendments);
					retval = new SqlStatementListExpression(newList);
				}
			}

			return retval;
		}
	}
}
