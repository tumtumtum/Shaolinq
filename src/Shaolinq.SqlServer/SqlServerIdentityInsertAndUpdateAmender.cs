// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)

using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Platform;
using Shaolinq.Persistence;
using Shaolinq.Persistence.Linq;
using Shaolinq.Persistence.Linq.Expressions;

namespace Shaolinq.SqlServer
{
	public class SqlServerIdentityInsertAndUpdateAmender
		: SqlExpressionVisitor
	{
		private readonly TypeDescriptorProvider typeDescriptorProvider;

		public SqlServerIdentityInsertAndUpdateAmender(TypeDescriptorProvider typeDescriptorProvider)
		{
			this.typeDescriptorProvider = typeDescriptorProvider;
		}

		public static Expression Amend(TypeDescriptorProvider typeDescriptorProvider, Expression expression)
		{
			return new SqlServerIdentityInsertAndUpdateAmender(typeDescriptorProvider).Visit(expression);
		}

		protected override Expression VisitInsertInto(SqlInsertIntoExpression expression)
		{
			if (!expression.RequiresIdentityInsert)
			{
				return base.VisitInsertInto(expression);
			}

			var list = new List<Expression>
			{
				new SqlSetCommandExpression("IDENTITY_INSERT", expression.Source, new SqlKeywordExpression("ON")),
				base.VisitInsertInto(expression),
				new SqlSetCommandExpression("IDENTITY_INSERT", expression.Source, new SqlKeywordExpression("OFF")),
			};

			return new SqlStatementListExpression(list);
		}

		protected override Expression VisitUpdate(SqlUpdateExpression expression)
		{
			if (!expression.RequiresIdentityInsert)
			{
				return base.VisitUpdate(expression);
			}

			var tableName = ((SqlTableExpression)expression.Source).Name;

			var typeDescriptor = this
				.typeDescriptorProvider
				.GetTypeDescriptors()
				.Single(c => c.PersistedName == tableName);

			var insertedColumns = expression
				.Assignments
				.OfType<SqlAssignExpression>()
				.Select(c => new { name = ((SqlColumnExpression)c.Target).Name, value = c.Value, propertyDescriptor = typeDescriptor.GetPropertyDescriptorByColumnName(((SqlColumnExpression)c.Target).Name) })
				.ToList();

			var columnInfos = QueryBinder
				.GetColumnInfos(this.typeDescriptorProvider, typeDescriptor.PersistedProperties.Where(c => insertedColumns.All(d => d.propertyDescriptor != c)))
				.ToList();

			var visitedUpdated = (SqlUpdateExpression)base.VisitUpdate(expression);
			var selectIntoExpression = new SqlSelectExpression
			(
				typeof(void),
				null,
				columnInfos.Select
				(
					c => new SqlColumnDeclaration
					(
						null, new SqlColumnExpression(c.DefinitionProperty.PropertyType, null, c.GetColumnName())
					)
				)
				.Concat(insertedColumns.Select(d => d.value.Type.GetUnwrappedNullableType() == typeof(bool) ? new SqlColumnDeclaration(d.name, new BitBooleanExpression(d.value)) : new SqlColumnDeclaration(d.name, d.value)))
				.ToReadOnlyCollection(),
				visitedUpdated.Source,
				visitedUpdated.Where,
				null, null, false, null, null, false, false, new SqlTableExpression("#TEMP")
			);

			var selectExpression = new SqlSelectExpression(typeof(void), null, null, selectIntoExpression.Into, null, null);
			var insertExpression = new SqlInsertIntoExpression(visitedUpdated.Source, columnInfos.Select(c => c.GetColumnName()).Concat(insertedColumns.Select(c => c.name)).ToReadOnlyCollection(), null, selectExpression, null, true);
			var deleteExpression = new SqlDeleteExpression(visitedUpdated.Source, visitedUpdated.Where);
			
			var list = new List<Expression>
			{
				selectIntoExpression,
				deleteExpression,
				new SqlSetCommandExpression("IDENTITY_INSERT", visitedUpdated.Source, new SqlKeywordExpression("ON")),
				insertExpression,
				new SqlSetCommandExpression("IDENTITY_INSERT", visitedUpdated.Source, new SqlKeywordExpression("OFF")),
			};

			return new SqlStatementListExpression(list);
		}
	}
}