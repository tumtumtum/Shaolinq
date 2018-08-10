// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Shaolinq.Persistence.Linq;
using Shaolinq.Persistence.Linq.Expressions;

namespace Shaolinq.SqlServer
{
	public class SqlServerUniqueNullIndexAnsiComplianceFixer
		: SqlExpressionVisitor
	{
		private class ColumnsToExcludeFinder
			: SqlExpressionVisitor
		{
			private HashSet<string> columnNames;

			public static HashSet<string> Find(Expression expression)
			{
				var finder = new ColumnsToExcludeFinder();

				finder.Visit(expression);

				return finder.columnNames;
			}

			protected override Expression VisitBinary(BinaryExpression binaryExpression)
			{
				if (binaryExpression.Left is SqlColumnExpression column1 && !IsNullableType(column1.Type))
				{
					(this.columnNames ?? (this.columnNames = new HashSet<string>())).Add(column1.Name);
				}
				else if (binaryExpression.Right is SqlColumnExpression column2 && !IsNullableType(column2.Type))
				{
					(this.columnNames ?? (this.columnNames = new HashSet<string>())).Add(column2.Name);
				}

				return base.VisitBinary(binaryExpression);
			}
		}
		
		private static bool IsNullableType(Type type)
		{
			return type.IsClass || Nullable.GetUnderlyingType(type) != null;
		}

		private readonly bool fixNonUniqueIndexesAsWell;
		private readonly bool explicitIndexConditionOverridesNullAnsiCompliance;

		private SqlServerUniqueNullIndexAnsiComplianceFixer(bool fixNonUniqueIndexesAsWell, bool explicitIndexConditionOverridesNullAnsiCompliance)
		{
			this.fixNonUniqueIndexesAsWell = fixNonUniqueIndexesAsWell;
			this.explicitIndexConditionOverridesNullAnsiCompliance = explicitIndexConditionOverridesNullAnsiCompliance;
		}

		public static Expression Fix(Expression expression, bool fixNonUniqueIndexesAsWell = false, bool explicitIndexConditionOverridesNullAnsiCompliance = false)
		{
			return new SqlServerUniqueNullIndexAnsiComplianceFixer(fixNonUniqueIndexesAsWell, explicitIndexConditionOverridesNullAnsiCompliance).Visit(expression);
		}

		protected override Expression VisitCreateIndex(SqlCreateIndexExpression createIndexExpression)
		{
			if (createIndexExpression.Where != null && this.explicitIndexConditionOverridesNullAnsiCompliance)
			{
				return createIndexExpression;
			}

		    if (!(createIndexExpression.Unique || this.fixNonUniqueIndexesAsWell))
		    {
		        return createIndexExpression;
		    }

			var columnsToExclude = createIndexExpression.Where == null ? null : ColumnsToExcludeFinder.Find(createIndexExpression.Where);

			var columnsToNullCheck = createIndexExpression
				.Columns
				.Where(c => IsNullableType(c.Column.Type))
				.Where(c => columnsToExclude == null || !columnsToExclude.Contains(c.Column.Name))
				.Select(c => (Expression)new SqlFunctionCallExpression(typeof(bool), SqlFunction.IsNotNull, c.Column))
				.ToList();

			var predicate = columnsToNullCheck.Count > 0 ? columnsToNullCheck.Aggregate(Expression.And) : null;

			if (predicate == null)
			{
				return createIndexExpression;
			}
			else
			{
				return createIndexExpression.ChangeWhere(createIndexExpression.Where == null ? predicate : Expression.And(createIndexExpression.Where, predicate));
			}
		}
	}
}
