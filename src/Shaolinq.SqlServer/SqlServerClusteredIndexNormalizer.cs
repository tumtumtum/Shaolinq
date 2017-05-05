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

			if (this.additionalStatements != null)
			{
				retval = retval.ChangeStatements(retval.Statements.Concat(this.additionalStatements));
			}

			return retval;
		}

		protected override Expression VisitConstraint(SqlConstraintExpression expression)
		{
			if (expression.PrimaryKey && this.makeUnclustered)
			{
				return expression.ChangeKeyOptions((expression.KeyOptions ?? new object[0]).Concat("NONCLUSTERED").ToArray());
			}

			return base.VisitConstraint(expression);
		}

		protected override Expression VisitCreateTable(SqlCreateTableExpression createTableExpression)
		{
			var retval = createTableExpression;
			var organizationIndex = createTableExpression.OrganizationIndex;

			if (organizationIndex == null)
			{
				return organizationIndex;
			}

			// No organization index - remove from primary key

			if (organizationIndex.Columns == null)
			{
				try
				{
					this.makeUnclustered = true;

					retval = ((SqlCreateTableExpression)base.VisitCreateTable(createTableExpression))
						.ChangeOrganizationIndex(null);

					return retval;
				}
				finally
				{
					this.makeUnclustered = false;
				}
			}

			// Organization index the same as primary key so just leave with SQL server default

			if (createTableExpression
				.TableConstraints
				.Where(c => c.PrimaryKey)
				.SelectMany(c => c.ColumnNames)
				.OrderBy(c => c)
				.SequenceEqual(organizationIndex.Columns.Select(c => c.Column.Name).OrderBy(c => c)))
			{
				retval = ((SqlCreateTableExpression)base.VisitCreateTable(createTableExpression))
					.ChangeOrganizationIndex(null);

				return retval;
			}

			// Custom organization index

			try
			{
				this.makeUnclustered = true;

				retval = ((SqlCreateTableExpression)base.VisitCreateTable(createTableExpression))
					.ChangeOrganizationIndex(null);
			}
			finally
			{
				this.makeUnclustered = false;
			}

			var indexExpression = new SqlCreateIndexExpression(organizationIndex.IndexName, createTableExpression.Table, false, false, IndexType.Default, false, organizationIndex.Columns, null, null, true);

			if (this.additionalStatements == null)
			{
				this.additionalStatements = new List<Expression>();
			}

			this.additionalStatements.Add(indexExpression);

			return retval;
		}
	}
}