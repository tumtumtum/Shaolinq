// Copyright (c) 2007-2014 Thong Nguyen (tumtumtum@gmail.com)

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq.Expressions;
using Shaolinq.Persistence.Linq.Expressions;

namespace Shaolinq.Persistence.Linq
{
	public class SqlForeignKeyConstraintToAlterAmmender
		: SqlExpressionVisitor
	{
		private bool foundStatementList = false;
		private SqlCreateTableExpression currentTable;
		private readonly List<Expression> ammendments = new List<Expression>();

		public static Expression Ammend(Expression expression)
		{
			var retval = new SqlForeignKeyConstraintToAlterAmmender().Visit(expression);

			return retval;
		}

		protected override Expression VisitForeignKeyConstraint(SqlForeignKeyConstraintExpression foreignKeyConstraintExpression)
		{
			var action = new SqlConstraintActionExpression(SqlConstraintActionType.Add, foreignKeyConstraintExpression);
			var ammendmentEpression = new SqlAlterTableExpression(currentTable.Table, action);

			ammendments.Add(ammendmentEpression);

			return null;
		}

		protected override Expression VisitCreateTable(SqlCreateTableExpression createTableExpression)
		{
			currentTable = createTableExpression;

			var retval = base.VisitCreateTable(createTableExpression);

			currentTable = null;

			return retval;
		}

		protected override Expression VisitStatementList(SqlStatementListExpression statementListExpression)
		{
			var localFoundStatementList = this.foundStatementList;

			this.foundStatementList = true;

			var retval = (SqlStatementListExpression)base.VisitStatementList(statementListExpression);

			if (!localFoundStatementList)
			{
				if (ammendments.Count > 0)
				{
					var newList = new List<Expression>(retval.Statements);
					
					newList.AddRange(ammendments);
					retval = new SqlStatementListExpression(new ReadOnlyCollection<Expression>(newList));
				}
			}

			return retval;
		}
	}
}
