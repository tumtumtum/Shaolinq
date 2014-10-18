// Copyright (c) 2007-2014 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
		private readonly ParameterExpression dataReader;
		private readonly ParameterExpression objectProjector;
		private readonly ParameterExpression dynamicParameters;
		private readonly DataAccessModel dataAccessModel;
		private readonly SqlDatabaseContext sqlDatabaseContext;
		private readonly Dictionary<string, int> columnIndexes;
		private static readonly MethodInfo ExecuteSubQueryMethod = typeof(ObjectProjector).GetMethod("ExecuteSubQuery");

		private ProjectionBuilder(DataAccessModel dataAccessModel, SqlDatabaseContext sqlDatabaseContext, IEnumerable<string> columns)
		{
			var x = 0;
			this.dataAccessModel = dataAccessModel;
			this.sqlDatabaseContext = sqlDatabaseContext;
			
			columnIndexes = columns.ToDictionary(c => c, c => x++);

			dataReader = Expression.Parameter(typeof(IDataReader), "dataReader");
			objectProjector = Expression.Parameter(typeof(ObjectProjector), "objectProjector");
			dynamicParameters = Expression.Parameter(typeof (object[]), "dynamicParameters");
		}

		/// <summary>
		/// Builds the lambda expression that will perform the projection
		/// </summary>
		/// <param name="dataAccessModel">The related data access model</param>
		/// <param name="sqlDatabaseContext">The related <see cref="SqlDatabaseContext"/></param>
		/// <param name="expression">
		/// The expression that performs the projection (can be any expression but usually is a MemberInit expression)
		/// </param>
		/// <returns>
		/// A <see cref="LambdaExpression"/> that takes two parameters: an <see cref="ObjectProjector"/>
		/// and an <see cref="IDataReader"/>.  The lambda expression will construct a single
		/// object for return from the current row in the given <see cref="IDataReader"/>.
		/// </returns>
		public static LambdaExpression Build(DataAccessModel dataAccessModel, SqlDatabaseContext sqlDatabaseContext, Expression expression, IEnumerable<string> columns)
		{
			var projectionBuilder = new ProjectionBuilder(dataAccessModel, sqlDatabaseContext, columns);

			var body = projectionBuilder.Visit(expression);
            
			return Expression.Lambda(body, projectionBuilder.objectProjector, projectionBuilder.dataReader, projectionBuilder.dynamicParameters);
		}

		private Type currentNewExpressionType = null;

		protected override Expression VisitMemberInit(MemberInitExpression expression)
		{
			var previousCurrentNewExpressionType = currentNewExpressionType;

			currentNewExpressionType = expression.NewExpression.Type;

			var retval = base.VisitMemberInit(expression);

			currentNewExpressionType = previousCurrentNewExpressionType;

			var submitToCacheMethod = typeof(IDataAccessObject).GetMethod("SubmitToCache", BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
			var resetModifiedMethod = typeof(IDataAccessObject).GetMethod("ResetModified", BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
			var finishedInitializingMethod = typeof(IDataAccessObject).GetMethod("FinishedInitializing", BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);

			return Expression.Convert(Expression.Call(Expression.Call(Expression.Call(retval, finishedInitializingMethod), resetModifiedMethod), submitToCacheMethod), retval.Type);
		}

		protected override Expression VisitConstantPlaceholder(SqlConstantPlaceholderExpression constantPlaceholder)
		{
			//return constantPlaceholder.ConstantExpression;
			//return Expression.ArrayIndex(this.dynamicParameters, Expression.Constant(constantPlaceholder.Index));
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

					if (propertyDescriptor.IsComputedTextMember)
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
				// Replace all new DataAccessObject() calls with new ConcreteDataAccessObject(DataAccessModel)

				return Expression.New
				(
					this.dataAccessModel.GetConcreteTypeFromDefinitionType(expression.Type).GetConstructor(new [] { typeof(DataAccessModel), typeof(bool) }),
					new Expression[] { Expression.Property(this.objectProjector, "DataAccessModel"), Expression.Constant(false) }
				);
			}

			var visitedArgs = VisitExpressionList(expression.Arguments);

			if (visitedArgs != expression.Arguments)
			{
				var i = 0;
				Expression[] newArgs = null;
                
				foreach (var newArg in visitedArgs)
				{
					if (newArg.Type != expression.Arguments[i].Type)
					{
						if (newArgs == null)
						{
							newArgs = new Expression[visitedArgs.Count];

							for (var j = 0; j < i; j++)
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

				return expression.Update(newArgs ?? (IEnumerable<Expression>)visitedArgs);
			}

			return expression;
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
					return (Expression)Expression.Condition(test, ifTrue, Expression.Convert(ifFalse, ifTrue.Type));
				}
				else
				{
					return (Expression)Expression.Condition(test, Expression.Convert(ifTrue, ifFalse.Type), ifFalse);
				}
			}
			else if (test != expression.Test || ifTrue != expression.IfTrue || ifFalse != expression.IfFalse)
			{
				return (Expression)Expression.Condition(test, ifTrue, ifFalse);
			}
			else
			{
				return (Expression)expression;
			}
		}

		protected override Expression VisitColumn(SqlColumnExpression column)
		{
			return this.ConvertColumnToDataReaderRead(column, column.Type);
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
			}

			return base.VisitUnary(unaryExpression);
		}

		protected virtual Expression ConvertColumnToDataReaderRead(SqlColumnExpression column, Type type)
		{
			// Replace all column accesses with the appropriate IDataReader call

			if (column.Type.IsDataAccessObjectType())
			{
				return Expression.Convert(Expression.Constant(null), column.Type);
			}
			else
			{
				var sqlDataType = this.sqlDatabaseContext.SqlDataTypeProvider.GetSqlDataType(type);

				if (!this.columnIndexes.ContainsKey(column.Name))
				{
					return sqlDataType.GetReadExpression(this.objectProjector, this.dataReader, 0);
				}
				else
				{
					return sqlDataType.GetReadExpression(this.objectProjector, this.dataReader, this.columnIndexes[column.Name]);
				}
			}
		}

		protected override Expression VisitObjectReference(SqlObjectReference sqlObjectReference)
		{
			var arrayOfValues = Expression.NewArrayInit(typeof(object), sqlObjectReference
				.Bindings
				.OfType<MemberAssignment>()
				.Select(c => (Expression)Expression.Convert(c.Expression.NodeType == (ExpressionType)SqlExpressionType.Column ? this.ConvertColumnToDataReaderRead((SqlColumnExpression)c.Expression, c.Expression.Type.MakeNullable()) : this.Visit(c.Expression), typeof(object))).ToArray());

			var method = MethodInfoFastRef.BaseDataAccessModelGetReferenceByPrimaryKeyWithPrimaryKeyValuesMethod.MakeGenericMethod(sqlObjectReference.Type);

			return Expression.Call(Expression.Property(this.objectProjector, "DataAccessModel"), method, arrayOfValues);
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
