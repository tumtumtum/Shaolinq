using System;
using System.Collections.Generic;
using System.Data;
using System.Linq.Expressions;
using System.Reflection;
using Shaolinq.Persistence.Sql.Linq.Expressions;
using Shaolinq.TypeBuilding;
using Platform;

namespace Shaolinq.Persistence.Sql.Linq
{
	public class ProjectionBuilder
		: SqlExpressionVisitor
	{
		private readonly ParameterExpression dataReader;
		private readonly ParameterExpression objectProjector;
		private readonly ParameterExpression dynamicParameters;
		private readonly BaseDataAccessModel dataAccessModel;
		private readonly PersistenceContext persistenceContext;
		private readonly Dictionary<string, int> columnIndexes;
		private static readonly MethodInfo ExecuteSubQueryMethod = typeof(ObjectProjector).GetMethod("ExecuteSubQuery");

		private ProjectionBuilder(BaseDataAccessModel dataAccessModel, PersistenceContext persistenceContext, IEnumerable<string> columns)
		{
			var x = 0;
			this.dataAccessModel = dataAccessModel;
			this.persistenceContext = persistenceContext;

			columnIndexes = columns.ToDictionaryWithKeys(c => x++);

			dataReader = Expression.Parameter(typeof(IDataReader), "dataReader");
			objectProjector = Expression.Parameter(typeof(ObjectProjector), "objectProjector");
			dynamicParameters = Expression.Parameter(typeof (object[]), "dynamicParameters");
		}

		/// <summary>
		/// Builds the lambda expression that will perform the projection
		/// </summary>
		/// <param name="dataAccessModel">The related data access model</param>
		/// <param name="persistenceContext">The related <see cref="PersistenceContext"/></param>
		/// <param name="expression">
		/// The expression that performs the projection (can be any expression but usually is a MemberInit expression)
		/// </param>
		/// <returns>
		/// A <see cref="LambdaExpression"/> that takes two parameters: an <see cref="ObjectProjector"/>
		/// and an <see cref="IDataReader"/>.  The lambda expression will construct a single
		/// object for return from the current row in the given <see cref="IDataReader"/>.
		/// </returns>
		public static LambdaExpression Build(BaseDataAccessModel dataAccessModel, PersistenceContext persistenceContext, Expression expression, IEnumerable<string> columns)
		{
			var projectionBuilder = new ProjectionBuilder(dataAccessModel, persistenceContext, columns);

			var body = projectionBuilder.Visit(expression);
            
			return Expression.Lambda(body, projectionBuilder.objectProjector, projectionBuilder.dataReader, projectionBuilder.dynamicParameters);
		}

		private Type currentNewExpressionType = null;

		protected override Expression VisitMemberInit(MemberInitExpression expression)
		{
			var previousCyrrentNewExpressionType = currentNewExpressionType;

			currentNewExpressionType = expression.NewExpression.Type;

			var retval = base.VisitMemberInit(expression);

			currentNewExpressionType = previousCyrrentNewExpressionType;

			return retval;
		}

		protected override Expression VisitConstantPlaceholder(SqlConstantPlaceholderExpression constantPlaceholder)
		{
			return Expression.Convert(Expression.ArrayIndex(this.dynamicParameters, Expression.Constant(constantPlaceholder.Index)), constantPlaceholder.ConstantExpression.Type);
		}

		protected override MemberAssignment VisitMemberAssignment(MemberAssignment assignment)
		{
			if (currentNewExpressionType != null)
			{
				// Turn all Object.Id expressions into Object.ForceId (to bypass validation checking)

				if (assignment.Member.DeclaringType.IsDataAccessObjectType())
				{
					var typeDescriptor = this.dataAccessModel.GetTypeDescriptor(currentNewExpressionType);
					var propertyDescriptor = typeDescriptor.GetPropertyDescriptorByPropertyName(assignment.Member.Name);

					if (propertyDescriptor.IsAutoIncrement || propertyDescriptor.IsPrimaryKey)
					{
						var concreteType = this.dataAccessModel.GetConcreteTypeFromDefinitionType(currentNewExpressionType);
						var propertyInfo = concreteType.GetProperty(DataAccessObjectTypeBuilder.ForceSetPrefix + assignment.Member.Name);
						var assignmentExpression = Visit(assignment.Expression);

						return Expression.Bind(propertyInfo, assignmentExpression);
					}
				}
			}

			return base.VisitMemberAssignment(assignment);
		}

		protected override Expression VisitFunctionCall(SqlFunctionCallExpression functionCallExpression)
		{
			if (functionCallExpression.Function == SqlFunction.Date)
			{
				return Expression.Call(Visit(functionCallExpression.Arguments[0]), typeof (DateTime).GetProperty("Date").GetGetMethod(), null);
			}
			
			return base.VisitFunctionCall(functionCallExpression);
		}

		protected override NewExpression VisitNew(NewExpression expression)
		{
			if (expression.Type.IsDataAccessObjectType())
			{
				// Replace all new DataAccessObject() calls with new ConcreteDataAccessObject(BaseDataAccessModel)

				return Expression.New
				(
					this.dataAccessModel.GetConcreteTypeFromDefinitionType(expression.Type).GetConstructor(new [] { typeof(BaseDataAccessModel) }),
					new Expression[] { Expression.Property(this.objectProjector, "DataAccessModel") }
				);
			}

			var visitedArgs = VisitExpressionList(expression.Arguments);

			if (visitedArgs != expression.Arguments)
			{
				int i = 0;
				Expression[] newArgs = null;
                
				foreach (var newArg in visitedArgs)
				{
					if (newArg.Type != expression.Arguments[i].Type)
					{
						if (newArgs == null)
						{
							newArgs = new Expression[visitedArgs.Count];

							for (int j = 0; j < i; j++)
							{
								newArgs[j] = visitedArgs[j];
							}
						}

						newArgs[i] = Expression.Convert(visitedArgs[i], expression.Arguments[i].Type);
					}
					else
					{
						if (newArgs != null)
						{
							newArgs[i] = visitedArgs[i];
						}
					}

					i++;
				}

				return Expression.New(expression.Constructor, (IEnumerable<Expression>)newArgs ?? visitedArgs, expression.Members);
			}

			return expression;
		}

		protected override Expression VisitColumn(SqlColumnExpression column)
		{
			// Replace all column accesses with the appropriate IDataReader call

			if (column.Type.IsDataAccessObjectType())
			{
				return Expression.Convert(Expression.Constant(null), column.Type);
			}
			else
			{
				var sqlDataType = persistenceContext.SqlDataTypeProvider.GetSqlDataType(column.Type);

				return sqlDataType.GetReadExpression(this.objectProjector, this.dataReader, this.columnIndexes[column.Name]);
			}
		}

		protected override Expression VisitProjection(SqlProjectionExpression projection)
		{
			var subQuery = Expression.Lambda(base.VisitProjection(projection), this.objectProjector);
			var elementType = TypeHelper.GetElementType(subQuery.Body.Type);
			var boundExecuteSubQueryMethod = ExecuteSubQueryMethod.MakeGenericMethod(elementType);

			return Expression.Convert(Expression.Call(this.objectProjector, boundExecuteSubQueryMethod, Expression.Constant(subQuery)), projection.Type);
		}
	}
}
