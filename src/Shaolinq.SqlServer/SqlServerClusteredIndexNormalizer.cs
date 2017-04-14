// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Platform;
using Shaolinq.Persistence.Linq;
using Shaolinq.Persistence.Linq.Expressions;

namespace Shaolinq.SqlServer
{
	public class SqlServerClusteredIndexNormalizer
		: SqlExpressionVisitor
	{
		private bool makeUnclustered;
		private List<Expression> additionalStatements;

		public static Expression Normalize(Expression expression)
		{
			return new SqlServerClusteredIndexNormalizer().Visit(expression);
		}

		protected override Expression VisitStatementList(SqlStatementListExpression statementListExpression)
		{
			var retval = (SqlStatementListExpression)base.VisitStatementList(statementListExpression);

			if (additionalStatements != null)
			{
				retval = retval.ChangeStatements(retval.Statements.Concat(additionalStatements));
			}

			return retval;
		}

		protected override Expression VisitConstraint(SqlConstraintExpression expression)
		{
			if (expression.PrimaryKey && makeUnclustered)
			{
				return expression.ChangeOptions(expression.ConstraintOptions.Concat("NONCLUSTERED").ToArray());
			}

			return base.VisitConstraint(expression);
		}

		protected override Expression VisitCreateTable(SqlCreateTableExpression createTableExpression)
		{
			var organizationIndex = createTableExpression.OrganizationIndex;
			
			if (organizationIndex != null)
			{
				if (organizationIndex.Columns == null)
				{
					try
					{
						this.makeUnclustered = true;

						return ((SqlCreateTableExpression)base.VisitCreateTable(createTableExpression)).ChangeOrganizationIndex(null);
					}
					finally
					{
						this.makeUnclustered = false;
					}
				}
				else
				{
					var indexExpression = new SqlCreateIndexExpression(organizationIndex.IndexName, createTableExpression.Table, false, false, IndexType.Default, true, organizationIndex.Columns, null);

					if (additionalStatements == null)
					{
						additionalStatements = new List<Expression>();
					}

					additionalStatements.Add(indexExpression);
				}
			}

			return createTableExpression;
		}
	}
}