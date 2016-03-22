// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Platform;
using Shaolinq.Persistence.Linq.Expressions;
using Shaolinq.Persistence.Linq.Optimizers;
using Shaolinq.TypeBuilding;

namespace Shaolinq.Persistence.Linq
{
	public class ProjectionBuilder
		: SqlExpressionVisitor
    {
	    private ProjectionBuilderScope scope;
        private Type currentNewExpressionType = null;
		private readonly ParameterExpression dataReader;
		private readonly ParameterExpression objectProjector;
		private readonly ParameterExpression dynamicParameters;
		private readonly DataAccessModel dataAccessModel;
		private readonly SqlDatabaseContext sqlDatabaseContext;
		private readonly SqlQueryProvider queryProvider;
		private readonly ParameterExpression versionParameter;
		
		private ProjectionBuilder(DataAccessModel dataAccessModel, SqlDatabaseContext sqlDatabaseContext, SqlQueryProvider queryProvider, ProjectionBuilderScope scope)
		{
			this.dataAccessModel = dataAccessModel;
			this.sqlDatabaseContext = sqlDatabaseContext;
			this.queryProvider = queryProvider;
			
			this.scope = scope;

			this.dataReader = Expression.Parameter(typeof(IDataReader), "dataReader");
			this.objectProjector = Expression.Parameter(typeof(ObjectProjector), "objectProjector");
			this.dynamicParameters = Expression.Parameter(typeof (object[]), "dynamicParameters");
			this.versionParameter = Expression.Parameter(typeof(int), "version");
		}

		public static LambdaExpression Build(DataAccessModel dataAccessModel, SqlDatabaseContext sqlDatabaseContext, SqlQueryProvider queryProvider, Expression expression, ProjectionBuilderScope scope)
		{
			var projectionBuilder = new ProjectionBuilder(dataAccessModel, sqlDatabaseContext, queryProvider, scope);

			var body = projectionBuilder.Visit(expression);
            
			return Expression.Lambda(body, projectionBuilder.objectProjector, projectionBuilder.dataReader, projectionBuilder.versionParameter, projectionBuilder.dynamicParameters);
		}

		protected override Expression VisitMemberInit(MemberInitExpression expression)
		{
			var previousCurrentNewExpressionType = this.currentNewExpressionType;

			this.currentNewExpressionType = expression.NewExpression.Type;

			Expression nullCheck = null;
			
			foreach (var value in SqlObjectOperandComparisonExpander.GetPrimaryKeyElementalExpressions(expression))
			{
				Expression current;

				if (value.NodeType == (ExpressionType)SqlExpressionType.Column)
				{
					current = this.ConvertColumnToIsNull((SqlColumnExpression)value);
				}
				else
				{
					var visited = this.Visit(value);

					if (visited.Type.IsClass || visited.Type.IsNullableType())
					{
						current = Expression.Equal(Expression.Convert(visited, visited.Type), Expression.Constant(null, visited.Type));
					}
					else
					{
						current = Expression.Equal(Expression.Convert(visited, visited.Type.MakeNullable()), Expression.Constant(null, visited.Type.MakeNullable()));
					}
				}

				if (nullCheck == null)
				{
					nullCheck = current;
				}
				else
				{
					nullCheck = Expression.Or(nullCheck, current);
				}
			}

			var retval = base.VisitMemberInit(expression);

			this.currentNewExpressionType = previousCurrentNewExpressionType;

			if (typeof(DataAccessObject).IsAssignableFrom(retval.Type))
			{
				var submitToCacheMethod = typeof(IDataAccessObjectInternal).GetMethod("SubmitToCache", BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
				var resetModifiedMethod = typeof(IDataAccessObjectInternal).GetMethod("ResetModified", BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
				var finishedInitializingMethod = typeof(IDataAccessObjectInternal).GetMethod("FinishedInitializing", BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);

				retval = Expression.Convert(Expression.Call(Expression.Call(Expression.Call(Expression.Convert(retval, typeof(IDataAccessObjectInternal)), finishedInitializingMethod), resetModifiedMethod), submitToCacheMethod), retval.Type);
			}

			if (nullCheck != null)
			{
				return Expression.Condition(nullCheck, Expression.Constant(null, retval.Type), retval);
			}
			else
			{
				return retval;
			}
		}

		protected override Expression VisitConstantPlaceholder(SqlConstantPlaceholderExpression constantPlaceholder)
		{
			return Expression.Convert(Expression.ArrayIndex(this.dynamicParameters, Expression.Constant(constantPlaceholder.Index)), constantPlaceholder.ConstantExpression.Type);
		}

		protected override MemberAssignment VisitMemberAssignment(MemberAssignment assignment)
		{
			if (this.currentNewExpressionType != null)
			{
				// Turn all Object.Id expressions into Object.ForceId (to bypass validation checking)

				if (assignment.Member.DeclaringType.IsDataAccessObjectType())
				{
					var typeDescriptor = this.dataAccessModel.GetTypeDescriptor(this.currentNewExpressionType);
					var propertyDescriptor = typeDescriptor.GetPropertyDescriptorByPropertyName(assignment.Member.Name);

					if (propertyDescriptor == null)
					{
						throw new InvalidOperationException($"Missing property: {assignment.Member.Name}");
					}

					if (propertyDescriptor.IsComputedTextMember || propertyDescriptor.IsComputedMember)
					{
						var concreteType = this.dataAccessModel.GetConcreteTypeFromDefinitionType(this.currentNewExpressionType);
						var propertyInfo = concreteType.GetProperty(DataAccessObjectTypeBuilder.ForceSetPrefix + assignment.Member.Name);
						var assignmentExpression = this.Visit(assignment.Expression);

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
				return Expression.Call(this.Visit(functionCallExpression.Arguments[0]), typeof(DateTime).GetProperty("Date").GetGetMethod(), null);
			}

			if (functionCallExpression.Function == SqlFunction.RecordsAffected)
			{
				return Expression.Property(this.dataReader, TypeUtils.GetProperty<IDataReader>(c => c.RecordsAffected));
			}

			return base.VisitFunctionCall(functionCallExpression);
		}

		protected override Expression VisitNew(NewExpression expression)
		{
			if (expression.Type.IsDataAccessObjectType())
			{
				// Replace all new DataAccessObject() calls with new ConcreteDataAccessObject(DataAccessModel)

				var constructor = this.dataAccessModel
					.GetConcreteTypeFromDefinitionType(expression.Type)
					.GetConstructor(new[] { typeof(DataAccessModel), typeof(bool) });

				if (constructor == null)
				{
					throw new InvalidOperationException(@"Missing constructor for {expression.Type}");
				}

                return Expression.New
				(
					constructor,
					Expression.Property(this.objectProjector, "DataAccessModel"),
					Expression.Constant(false)
				);
			}

			return base.VisitNew(expression);
		}

		protected override Expression VisitConditional(ConditionalExpression expression)
		{
			var test = this.Visit(expression.Test);
			var ifTrue = this.Visit(expression.IfTrue);
			var ifFalse = this.Visit(expression.IfFalse);

			if (ifTrue.Type != ifFalse.Type)
			{
				if (ifTrue.Type.IsDataAccessObjectType() && !ifTrue.Type.IsAbstract)
				{
					return Expression.Condition(test, ifTrue, Expression.Convert(ifFalse, ifTrue.Type));
				}
				else
				{
					return Expression.Condition(test, Expression.Convert(ifTrue, ifFalse.Type), ifFalse);
				}
			}
			else if (test != expression.Test || ifTrue != expression.IfTrue || ifFalse != expression.IfFalse)
			{
				return Expression.Condition(test, ifTrue, ifFalse);
			}
			else
			{
				return expression;
			}
		}

		protected override Expression VisitColumn(SqlColumnExpression column)
		{
			return this.ConvertColumnToDataReaderRead(column, column.Type);
		}

		protected override Expression VisitMethodCall(MethodCallExpression methodCallExpression)
		{
			if (methodCallExpression.Method == MethodInfoFastRef.TransactionContextGetCurrentContextVersion)
			{
				return this.versionParameter;
			}

			var retval = base.VisitMethodCall(methodCallExpression);

			var type = retval.Type;

			if (!type.IsDataAccessObjectType())
			{
				return retval;
			}

			var concrete = this.dataAccessModel.GetConcreteTypeFromDefinitionType(type);

			if (concrete == type)
			{
				return retval;
			}

			return Expression.Convert(retval, concrete);
		}

		protected override Expression VisitUnary(UnaryExpression unaryExpression)
		{
			if (unaryExpression.NodeType == ExpressionType.Convert)
			{
				if (unaryExpression.Operand.NodeType == ((ExpressionType)SqlExpressionType.Column)
					&& unaryExpression.Type == unaryExpression.Operand.Type.MakeNullable())
				{
					return this.ConvertColumnToDataReaderRead((SqlColumnExpression)unaryExpression.Operand, unaryExpression.Operand.Type.MakeNullable());
				}
				else if (unaryExpression.Type.IsDataAccessObjectType())
				{
					var concrete = this.dataAccessModel.GetConcreteTypeFromDefinitionType(unaryExpression.Type);

					if (concrete != unaryExpression.Type)
					{
						return Expression.Convert(this.Visit(unaryExpression.Operand), concrete);
					}
				}
			}

			return base.VisitUnary(unaryExpression);
		}

		protected virtual Expression ConvertColumnToIsNull(SqlColumnExpression column)
		{
			var sqlDataType = this.sqlDatabaseContext.SqlDataTypeProvider.GetSqlDataType(column.Type);

			if (!this.scope.ColumnIndexes.ContainsKey(column.Name))
			{
				return sqlDataType.IsNullExpression(this.dataReader, 0);
			}
			else
			{
				return sqlDataType.IsNullExpression(this.dataReader, this.scope.ColumnIndexes[column.Name]);
			}
		}

		protected virtual Expression ConvertColumnToDataReaderRead(SqlColumnExpression column, Type type)
		{
			if (column.Type.IsDataAccessObjectType())
			{
				return Expression.Convert(Expression.Constant(null), column.Type);
			}
			else
			{
				var sqlDataType = this.sqlDatabaseContext.SqlDataTypeProvider.GetSqlDataType(type);

				if (!this.scope.ColumnIndexes.ContainsKey(column.Name))
				{
					throw new InvalidOperationException($"Unable to find matching column reference named {column.Name}");
				}
				else
				{
					return sqlDataType.GetReadExpression(this.dataReader, this.scope.ColumnIndexes[column.Name]);
				}
			}
		}

		protected override Expression VisitObjectReference(SqlObjectReferenceExpression sqlObjectReferenceExpression)
		{
			var arrayOfValues = Expression.NewArrayInit(typeof(object), sqlObjectReferenceExpression
				.Bindings
				.OfType<MemberAssignment>()
				.Select(c => (Expression)Expression.Convert(c.Expression.NodeType == (ExpressionType)SqlExpressionType.Column ? this.ConvertColumnToDataReaderRead((SqlColumnExpression)c.Expression, c.Expression.Type.MakeNullable()) : this.Visit(c.Expression), typeof(object))).ToArray());

			var method = MethodInfoFastRef.DataAccessModelGetReferenceByPrimaryKeyWithPrimaryKeyValuesMethod.MakeGenericMethod(sqlObjectReferenceExpression.Type);

			return Expression.Call(Expression.Property(this.objectProjector, nameof(ObjectProjector.DataAccessModel)), method, arrayOfValues);
		}

		protected override Expression VisitProjection(SqlProjectionExpression projectionExpression)
		{
			if (typeof(RelatedDataAccessObjects<>).IsAssignableFromIgnoreGenericParameters(projectionExpression.Type))
			{
				var elementType = projectionExpression.Type.GetGenericArguments()[0];
				var originalPlaceholderCount = 0;
				var currentPlaceholderCount = originalPlaceholderCount;

				var replacedExpressions = new List<Expression>();
				projectionExpression = (SqlProjectionExpression)SqlOuterQueryReferencePlaceholderSubstitutor.Substitute(projectionExpression, ref currentPlaceholderCount, replacedExpressions);
				var values = replacedExpressions.Select(c => Expression.Convert(this.Visit(c), typeof(object))).ToList();
				var where = projectionExpression.Select.Where;

				var typeDescriptor = this.dataAccessModel.TypeDescriptorProvider.GetTypeDescriptor(elementType);
				var columns = QueryBinder.GetColumnInfos(this.dataAccessModel.TypeDescriptorProvider, typeDescriptor.PersistedProperties);
				
				var columnExpression = (SqlColumnExpression)SqlExpressionFinder.FindFirst(where, c => c.NodeType == (ExpressionType)SqlExpressionType.Column);
				var match = columns.Single(d => d.ColumnName == columnExpression.Name);

				var reference = Expression.Call(Expression.Constant(this.dataAccessModel), MethodInfoFastRef.DataAccessModelGetReferenceByValuesMethod.MakeGenericMethod(match.ForeignType.Type), Expression.NewArrayInit(typeof(object), values));
				var property = typeDescriptor.GetRelationshipInfos().Single(c => c.ReferencingProperty == match.RootProperty).TargetProperty;

                return Expression.Convert(Expression.Property(reference, property), this.dataAccessModel.GetConcreteTypeFromDefinitionType(property.PropertyType));
			}
			else
			{
				var currentPlaceholderCount = 0;
				var replacedExpressions = new List<Expression>();
				projectionExpression = (SqlProjectionExpression)SqlOuterQueryReferencePlaceholderSubstitutor.Substitute(projectionExpression, ref currentPlaceholderCount, replacedExpressions);

				var newColumnIndexes = projectionExpression.Select.Columns.Select((c, i) => new { c.Name, i }).ToDictionary(d => d.Name, d => d.i);

				var savedScope = this.scope;
				this.scope = new ProjectionBuilderScope(newColumnIndexes);
				var projectionProjector = Expression.Lambda(this.Visit(projectionExpression.Projector), objectProjector, dataReader, versionParameter, dynamicParameters);
				this.scope = savedScope;

				var values = replacedExpressions.Select(c => (Expression)Expression.Convert(Visit(c), typeof(object))).ToList();

				var method = TypeUtils.GetMethod<SqlQueryProvider>(c => c.BuildExecution(default(SqlProjectionExpression), default(LambdaExpression), default(object[])));

				MethodInfo evaluate;

				if (projectionExpression.Type.GetSequenceElementType() == null)
				{
					evaluate = MethodInfoFastRef.ExecutionBuildResultEvaluateMethod.MakeGenericMethod(projectionExpression.Type);
				}
				else
				{
					evaluate = MethodInfoFastRef.ExecutionBuildResultEvaluateMethod.MakeGenericMethod(typeof(IEnumerable<>).MakeGenericType(projectionExpression.Type.GetSequenceElementType()));
				}
				
				return Expression.Call(Expression.Call(Expression.Property(this.objectProjector, "QueryProvider"), method, Expression.Constant(projectionExpression), projectionProjector, Expression.NewArrayInit(typeof(object), values)), evaluate);
			}
		}
	}
}
