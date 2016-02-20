// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Platform;
using Shaolinq.Persistence.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Optimizers
{
	public class SqlSubCollectionOrderByAmender
		: SqlExpressionVisitor
	{
		private readonly DataAccessModel dataAccessModel;
		private SqlProjectionExpression currentProjection;

		private SqlSubCollectionOrderByAmender(DataAccessModel dataAccessModel)
		{
			this.dataAccessModel = dataAccessModel;
		}

		public static Expression Amend(DataAccessModel dataAccessModel, Expression expression)
		{
			return new SqlSubCollectionOrderByAmender(dataAccessModel).Visit(expression);
		}

		protected override Expression VisitSelect(SqlSelectExpression selectExpression)
		{
			if (this.currentProjection == null || this.currentProjection.Select != selectExpression)
			{
				return base.VisitSelect(selectExpression);
			}

			var objectType = selectExpression.Type.GetSequenceElementType();

			if (!objectType.IsDataAccessObjectType())
			{
				return base.VisitSelect(selectExpression);
			}

			var aliasesAndTypes = SqlAliasTypeCollector.Collect(selectExpression)
				.ToDictionary(c => c.Item1, c => dataAccessModel.TypeDescriptorProvider.GetTypeDescriptor(c.Item2.GetSequenceElementType() ?? c.Item2));

			List<Expression> orderBys = null;
			var includeJoins = selectExpression.From.GetIncludeJoins().ToList();
			List<SqlColumnExpression> leftMostColumns = null;

			foreach (var includeJoin in includeJoins)
			{
				var equalsExpression = (BinaryExpression)SqlExpressionFinder.FindFirst(includeJoin, c => c.NodeType == ExpressionType.Equal);

				var left = (SqlColumnExpression)equalsExpression.Left;
				var right = (SqlColumnExpression)equalsExpression.Right;

				var leftType  = aliasesAndTypes[left.SelectAlias];
				var rightType = aliasesAndTypes[right.SelectAlias];

				if (leftMostColumns == null)
				{
					var typeDescriptor = this.dataAccessModel.TypeDescriptorProvider.GetTypeDescriptor(objectType);
					var primaryKeyColumns = new HashSet<string>(QueryBinder.GetPrimaryKeyColumnInfos(this.dataAccessModel.TypeDescriptorProvider, typeDescriptor).Select(c => c.ColumnName));
					
					leftMostColumns = primaryKeyColumns
						.Select(c => new SqlColumnExpression(objectType, left.SelectAlias, c))
						.ToList();
				}

				var rightProperty = rightType.GetPropertyDescriptorByColumnName(right.Name);
				var leftProperty = rightProperty.RelationshipInfo?.TargetProperty ?? leftType.GetPropertyDescriptorByColumnName(left.Name);

				if (leftProperty.PropertyType.GetGenericTypeDefinitionOrNull() == typeof(RelatedDataAccessObjects<>))
				{
					var rightColumns = SqlExpressionFinder.FindAll(includeJoin, c => c.NodeType == (ExpressionType)SqlExpressionType.Column && ((SqlColumnExpression)c).SelectAlias == right.SelectAlias);
					var leftColumns = SqlExpressionFinder.FindAll(includeJoin, c => c.NodeType == (ExpressionType)SqlExpressionType.Column && ((SqlColumnExpression)c).SelectAlias == left.SelectAlias);

					if (orderBys == null)
					{
						orderBys = new List<Expression>();

						if (selectExpression.OrderBy?.Count > 0)
						{
							orderBys.AddRange(selectExpression.OrderBy);
							orderBys.AddRange(leftMostColumns.Select(c => new SqlOrderByExpression(OrderType.Ascending, c)));
						}
					}

					orderBys.AddRange(rightColumns.Select(c => new SqlOrderByExpression(OrderType.Ascending, c)));
					orderBys.AddRange(leftColumns.Select(c => new SqlOrderByExpression(OrderType.Ascending, c)));
				}
			}

			return selectExpression.ChangeOrderBy(orderBys?.Distinct(SqlExpressionEqualityComparer.Default) ?? selectExpression.OrderBy);
		}

		protected override Expression VisitProjection(SqlProjectionExpression projection)
		{
			var saveProjection = this.currentProjection;

			try
			{
				this.currentProjection = projection;

				return base.VisitProjection(projection);
			}
			finally
			{
				this.currentProjection = saveProjection;
			}
		}
	}
}
