// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

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
		private TypeDescriptor currentNewExpressionTypeDescriptor;
		private readonly ParameterExpression dataReader;
		private readonly ParameterExpression objectProjector;
		private readonly ParameterExpression dynamicParameters;
		private readonly DataAccessModel dataAccessModel;
		private readonly SqlDatabaseContext sqlDatabaseContext;
		private readonly SqlQueryProvider queryProvider;
		private readonly ParameterExpression versionParameter;
		private readonly ParameterExpression filterParameter;

		private bool atRootLevel = true;
		private bool treatColumnsAsNullable;

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
			this.filterParameter = Expression.Parameter(typeof(Func<DataAccessObject, DataAccessObject>), "filter");
		}

		public static LambdaExpression Build(DataAccessModel dataAccessModel, SqlDatabaseContext sqlDatabaseContext, SqlQueryProvider queryProvider, Expression expression, ProjectionBuilderScope scope, out Expression<Func<IDataReader, object[]>> rootKeys)
		{
			var projectionBuilder = new ProjectionBuilder(dataAccessModel, sqlDatabaseContext, queryProvider, scope);

			var body = projectionBuilder.Visit(expression);

			if (projectionBuilder.scope.rootPrimaryKeys.Count > 0)
			{
				rootKeys = Expression.Lambda<Func<IDataReader, object[]>>(Expression.NewArrayInit(typeof(object), projectionBuilder.scope.rootPrimaryKeys), projectionBuilder.dataReader);
			}
			else
			{
				rootKeys = null;
			}

			return Expression.Lambda(body, projectionBuilder.objectProjector, projectionBuilder.dataReader, projectionBuilder.versionParameter, projectionBuilder.dynamicParameters, projectionBuilder.filterParameter);
		}

		protected override Expression VisitNewArray(NewArrayExpression expression)
		{
			if (expression.Type.GetArrayRank() == 1 && !expression.Type.GetElementType().IsDataAccessObjectType())
			{
				if (expression.Expressions.All(c => c.Type.IsDataAccessObjectType()))
				{
					if (expression.NodeType == ExpressionType.NewArrayInit)
					{
						return Expression.NewArrayInit(typeof(DataAccessObject), this.VisitExpressionList(expression.Expressions));
					}
					else
					{
						return Expression.NewArrayBounds(typeof(DataAccessObject), this.VisitExpressionList(expression.Expressions));
					}
				}
			}

			return base.VisitNewArray(expression);
		}

		protected override Expression VisitMemberInit(MemberInitExpression expression)
		{
			Expression nullCheck = null;
			var savedRootLevel = this.atRootLevel;
			
			try
			{
				if (this.atRootLevel)
				{
					this.atRootLevel = false;

					if (typeof(DataAccessObject).IsAssignableFrom(expression.NewExpression.Type))
					{
						foreach (var value in SqlObjectOperandComparisonExpander.GetPrimaryKeyElementalExpressions(expression))
						{
							this.treatColumnsAsNullable = true;
							var visited = this.Visit(value);
							this.treatColumnsAsNullable = false;

							this.scope.rootPrimaryKeys.Add(visited.Type.IsValueType ? Expression.Convert(visited, typeof(object)) : visited);
						}
					}
				}

				var previousCurrentNewExpressionTypeDescriptor = this.currentNewExpressionTypeDescriptor;

				this.currentNewExpressionTypeDescriptor = this.dataAccessModel.TypeDescriptorProvider.GetTypeDescriptor(expression.NewExpression.Type);

				if (typeof(DataAccessObject).IsAssignableFrom(expression.NewExpression.Type))
				{
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
				}

				var retval = base.VisitMemberInit(expression);

				this.currentNewExpressionTypeDescriptor = previousCurrentNewExpressionTypeDescriptor;

				if (typeof(DataAccessObject).IsAssignableFrom(retval.Type))
				{
					var submitToCacheMethod = TypeUtils.GetMethod<IDataAccessObjectInternal>(c => c.SubmitToCache());
					var resetModifiedMethod = TypeUtils.GetMethod<IDataAccessObjectInternal>(c => c.ResetModified());
					var finishedInitializingMethod = TypeUtils.GetMethod<IDataAccessObjectInternal>(c => c.FinishedInitializing());

					retval = Expression.Convert(Expression.Invoke(this.filterParameter, Expression.Convert(Expression.Call(Expression.Call(Expression.Call(Expression.Convert(retval, typeof(IDataAccessObjectInternal)), finishedInitializingMethod), resetModifiedMethod), submitToCacheMethod), typeof(DataAccessObject))), retval.Type);
				}

				if (nullCheck != null)
				{
					return Expression.Condition(nullCheck, Expression.Convert(Expression.Invoke(this.filterParameter, Expression.Constant(null, retval.Type)), retval.Type), retval);
				}
				else
				{
					return retval;
				}
			}
			finally
			{
				this.atRootLevel = savedRootLevel;
			}
		}

		protected override Expression VisitConstantPlaceholder(SqlConstantPlaceholderExpression constantPlaceholder)
		{
			return Expression.Convert(Expression.ArrayIndex(this.dynamicParameters, Expression.Constant(constantPlaceholder.Index)), constantPlaceholder.ConstantExpression.Type);
		}

		protected override MemberAssignment VisitMemberAssignment(MemberAssignment assignment)
		{
			if (this.currentNewExpressionTypeDescriptor != null)
			{
				// Turn all Object.Id expressions into Object.ForceId (to bypass validation checking)

				if (assignment.Member.DeclaringType.IsDataAccessObjectType())
				{
					var typeDescriptor = this.currentNewExpressionTypeDescriptor;
					var propertyDescriptor = typeDescriptor.GetPropertyDescriptorByPropertyName(assignment.Member.Name);

					if (propertyDescriptor == null)
					{
						throw new InvalidOperationException($"Missing property: {assignment.Member.Name}");
					}

					if (propertyDescriptor.IsComputedTextMember || propertyDescriptor.IsComputedMember)
					{
						var concreteType = this.dataAccessModel.GetConcreteTypeFromDefinitionType(this.currentNewExpressionTypeDescriptor.Type);
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
			if (this.treatColumnsAsNullable && column.Type.IsValueType && !column.Type.IsNullableType())
			{
				return this.ConvertColumnToDataReaderRead(column, column.Type.MakeNullable());
			}
			else
			{
				return this.ConvertColumnToDataReaderRead(column, column.Type);
			}
		}

		protected override Expression VisitMethodCall(MethodCallExpression methodCallExpression)
		{
			if (methodCallExpression.Method == MethodInfoFastRef.TransactionContextGetCurrentContextVersion)
			{
				return this.versionParameter;
			}

			Expression retval;

			if (methodCallExpression.Method.IsGenericMethod
				&& methodCallExpression.Method.GetGenericMethodDefinition() == MethodInfoFastRef.DataAccessObjectExtensionsAddToCollectionMethod)
			{
				var instance = this.Visit(methodCallExpression.Object);
				var firstArg = this.Visit(methodCallExpression.Arguments[0]);

				var savedAtRootLevel = this.atRootLevel;
				this.atRootLevel = false;
				var arguments = (IEnumerable<Expression>)this.VisitExpressionList(methodCallExpression.Arguments.Skip(1).ToArray());
				this.atRootLevel = savedAtRootLevel;
				
				retval = Expression.Call(instance, methodCallExpression.Method, arguments.Prepend(firstArg));
			}
			else
			{
				retval = base.VisitMethodCall(methodCallExpression);
			}
			
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

				if (unaryExpression.Type.IsDataAccessObjectType())
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

			var sqlDataType = this.sqlDatabaseContext.SqlDataTypeProvider.GetSqlDataType(type);

			if (!this.scope.ColumnIndexes.ContainsKey(column.Name))
			{
				throw new InvalidOperationException($"Unable to find matching column reference named {column.Name}");
			}

			return sqlDataType.GetReadExpression(this.dataReader, this.scope.ColumnIndexes[column.Name]);
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
				var projectionProjector = Expression.Lambda(this.Visit(projectionExpression.Projector), this.objectProjector, this.dataReader, this.versionParameter, this.dynamicParameters, this.filterParameter);

				Expression rootKeys;

				if (this.scope.rootPrimaryKeys.Count > 0)
				{
					rootKeys = Expression.Quote(Expression.Lambda<Func<IDataReader, object[]>>(Expression.NewArrayInit(typeof(object), this.scope.rootPrimaryKeys), this.dataReader));
				}
				else
				{
					rootKeys = Expression.Constant(null, typeof(Expression<Func<IDataReader, object[]>>));
				}

				this.scope = savedScope;

				var values = replacedExpressions.Select(c => (Expression)Expression.Convert(this.Visit(c), typeof(object))).ToList();

				var method = TypeUtils.GetMethod<SqlQueryProvider>(c => c.BuildExecution(default(SqlProjectionExpression), default(LambdaExpression), default(object[]), default(Expression<Func<IDataReader, object[]>>)));

				MethodInfo evaluate;

				if (projectionExpression.Type.GetSequenceElementType() == null)
				{
					evaluate = MethodInfoFastRef.ExecutionBuildResultEvaluateMethod.MakeGenericMethod(projectionExpression.Type);
				}
				else
				{
					evaluate = MethodInfoFastRef.ExecutionBuildResultEvaluateMethod.MakeGenericMethod(typeof(IEnumerable<>).MakeGenericType(projectionExpression.Type.GetSequenceElementType()));
				}
				
				return Expression.Call(Expression.Call(Expression.Property(this.objectProjector, "QueryProvider"), method, Expression.Constant(SqlAggregateProjectionNormalizer.Normalize(projectionExpression)), projectionProjector, Expression.NewArrayInit(typeof(object), values), rootKeys), evaluate);
			}
		}
	}
}
