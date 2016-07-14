using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using Platform;
using Shaolinq.Persistence.Linq.Expressions;
using Shaolinq.Persistence.Linq.Optimizers;
using Shaolinq.TypeBuilding;

namespace Shaolinq.Persistence.Linq
{
	public class RootDataAccessObjectPrimaryKeysProjectorBuilder
		: SqlExpressionVisitor
	{
		private bool alreadyInMemberInit;
		private readonly SqlDatabaseContext sqlDatabaseContext;
		private readonly ProjectionBuilderScope scope;
		private readonly ParameterExpression dataReader;
		private readonly List<Expression> values = new List<Expression>();

		private RootDataAccessObjectPrimaryKeysProjectorBuilder(SqlDatabaseContext sqlDatabaseContext, ProjectionBuilderScope scope)
		{
			this.sqlDatabaseContext = sqlDatabaseContext;
			this.scope = scope;
			this.dataReader = Expression.Parameter(typeof(IDataReader), "dataReader");
		}

		public static Expression<Func<IDataReader, object[]>> Build(SqlDatabaseContext sqlDatabaseContext, ProjectionBuilderScope scope, Expression expression)
		{
			var builder = new RootDataAccessObjectPrimaryKeysProjectorBuilder(sqlDatabaseContext, scope);

			builder.Visit(expression);

			if (builder.values.Count == 0 )
			{
				return null;
			}

			return Expression.Lambda<Func<IDataReader, object[]>>(Expression.NewArrayInit(typeof(object), builder.values), builder.dataReader);
		}

		protected override Expression VisitColumn(SqlColumnExpression column)
		{
			if (this.alreadyInMemberInit)
			{
				return column;
			}

			return this.ConvertColumnToDataReaderRead(column, column.Type);
		}

		protected virtual Expression ConvertColumnToDataReaderRead(SqlColumnExpression column, Type type)
		{
			if (column.Type.IsDataAccessObjectType())
			{
				return Expression.Convert(Expression.Constant(null), column.Type);
			}

			var sqlDataType = this.sqlDatabaseContext.SqlDataTypeProvider.GetSqlDataType(type);

			if (!this.scope.ColumnIndexes.ContainsKey(column.Name))
			{
				throw new InvalidOperationException($"Unable to find matching column reference named {column.Name}");
			}

			return sqlDataType.GetReadExpression(this.dataReader, this.scope.ColumnIndexes[column.Name]);
		}

		protected override Expression VisitMemberInit(MemberInitExpression expression)
		{
			if (this.alreadyInMemberInit)
			{
				return expression;
			}

			this.alreadyInMemberInit = true;

			try
			{
				if (typeof(DataAccessObject).IsAssignableFrom(expression.NewExpression.Type))
				{
					foreach (var value in SqlObjectOperandComparisonExpander.GetPrimaryKeyElementalExpressions(expression))
					{
						var visited = this.Visit(value);

						values.Add(visited.Type.IsValueType ? Expression.Convert(visited, typeof(object)) : visited);
					}

					return expression;
				}
				else
				{
					return base.VisitMemberInit(expression);
				}
			}
			finally
			{
				this.alreadyInMemberInit = false;
			}
		}

		protected override Expression VisitMethodCall(MethodCallExpression methodCallExpression)
		{
			if (methodCallExpression.Method.IsGenericMethod
				&& methodCallExpression.Method.GetGenericMethodDefinition() == MethodInfoFastRef.DataAccessObjectExtensionsAddToCollectionMethod)
			{
				this.Visit(methodCallExpression.Arguments[0]);

				return methodCallExpression;
			}
			else
			{
				return base.VisitMethodCall(methodCallExpression);
			}
		}

		private int projectionNest = 0;

		protected override Expression VisitProjection(SqlProjectionExpression projection)
		{
			if (projectionNest > 0)
			{
				return projection;
			}

			projectionNest++;

			try
			{
				return base.VisitProjection(projection);
			}
			finally
			{
				projectionNest--;
			}
		}
	}
}
