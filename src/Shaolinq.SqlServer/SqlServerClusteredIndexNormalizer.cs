// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Shaolinq.Persistence.Linq;
using Shaolinq.Persistence.Linq.Expressions;

namespace Shaolinq.SqlServer
{
	public class SqlServerClusteredIndexNormalizer
		: SqlExpressionVisitor
	{
		public static Expression Normalize(Expression expression)
		{
			return new SqlServerClusteredIndexNormalizer().Visit(expression);
		}

		protected override Expression VisitCreateTable(SqlCreateTableExpression createTableExpression)
		{
			var organizationIndex = createTableExpression.OrganizationIndex;
			
			if (organizationIndex != null)
			{
				if (organizationIndex.Columns == null)
				{
					var primaryKeyConstraint = createTableExpression.TableConstraints.FirstOrDefault(c => c.PrimaryKey);

					if (primaryKeyConstraint != null)
					{
						//var newPrimaryKeyConstraint = primaryKeyConstraint.
						//return createTableExpression.ChangeConstraints(newConstraints);
					}
				}
			}

			return base.VisitCreateTable(createTableExpression);
		}
	}
}