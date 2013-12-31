// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Shaolinq.Persistence.Linq.Expressions;
using Platform;
using Platform.Reflection;
using Shaolinq.TypeBuilding;

namespace Shaolinq.Persistence.Linq
{
	public class QueryBinder
		: Platform.Linq.ExpressionVisitor
	{
		private int aliasCount;
		private bool isWithinClientSideCode;
		private readonly Type conditionType;
		private LambdaExpression extraCondition;
		private readonly Expression rootExpression;
		private List<SqlOrderByExpression> thenBys;
		private readonly TypeDescriptorProvider typeDescriptorProvider;
		private readonly Dictionary<Expression, GroupByInfo> groupByMap;
		private readonly Dictionary<ParameterExpression, Expression> expressionsByParameter;
		
		public DataAccessModel DataAccessModel { get; private set; }

		protected void AddExpressionByParameter(ParameterExpression parameterExpression, Expression expression)
		{
			expressionsByParameter[parameterExpression] = expression;
		}

		private QueryBinder(DataAccessModel dataAccessModel, Expression rootExpression, Type conditionType, LambdaExpression extraCondition)
		{
			this.conditionType = conditionType;
			this.DataAccessModel = dataAccessModel;
			this.rootExpression = rootExpression;
			this.extraCondition = extraCondition;
			this.typeDescriptorProvider = TypeDescriptorProvider.GetProvider(dataAccessModel.DefinitionAssembly);

			expressionsByParameter = new Dictionary<ParameterExpression, Expression>();
			groupByMap = new Dictionary<Expression, GroupByInfo>();
		}

		public static Expression Bind(DataAccessModel dataAccessModel, Expression expression, Type conditionType, LambdaExpression extraCondition)
		{
			var expandedExpression = RelatedPropertiesJoinExpander.Expand(dataAccessModel, expression);

			var queryBinder = new QueryBinder(dataAccessModel, expandedExpression, conditionType, extraCondition);

			return queryBinder.Visit(expandedExpression);
		}

		private static bool CanBeColumn(Expression expression)
		{
			switch (expression.NodeType)
			{
				case (ExpressionType)SqlExpressionType.Column:
				case (ExpressionType)SqlExpressionType.Subquery:
				case (ExpressionType)SqlExpressionType.AggregateSubquery:
				case (ExpressionType)SqlExpressionType.Aggregate:
					return true;
				case (ExpressionType)SqlExpressionType.ObjectOperand:
					return true;
				default:
					return false;
			}
		}

		internal static Expression StripQuotes(Expression expression)
		{
			while (expression.NodeType == ExpressionType.Quote)
			{
				expression = ((UnaryExpression)expression).Operand;
			}

			return expression;
		}

		public static ForeignKeyColumnInfo[] ExpandPropertyIntoForeignKeyColumns(DataAccessModel model, TypeDescriptor typeDescriptor, string namePrefix)
		{
			var retval = new ForeignKeyColumnInfo[typeDescriptor.PrimaryKeyProperties.Count];

			var i = 0;

			foreach (var relatedPropertyDescriptor in typeDescriptor.PrimaryKeyProperties)
			{
				retval[i] = new ForeignKeyColumnInfo
				{
					ForeignType = typeDescriptor,
					ColumnName = namePrefix + relatedPropertyDescriptor.PersistedShortName,
					KeyPropertyOnForeignType = relatedPropertyDescriptor
				};

				i++;
			}

			return retval;
		}

		public static ForeignKeyColumnInfo[] ExpandPropertyIntoForeignKeyColumns(DataAccessModel dataAccessModel, PropertyDescriptor propertyDescriptor)
		{
			if (propertyDescriptor.IsBackReferenceProperty || propertyDescriptor.PersistedMemberAttribute != null && propertyDescriptor.PropertyType.IsDataAccessObjectType())
			{
				var i = 0;
				var typeDescriptor = dataAccessModel.GetTypeDescriptor(propertyDescriptor.PropertyType);

				var retval = new ForeignKeyColumnInfo[typeDescriptor.PrimaryKeyProperties.Count];

				foreach (var relatedPropertyDescriptor in typeDescriptor.PrimaryKeyProperties)
				{
					retval[i] = new ForeignKeyColumnInfo
					{
						ForeignType = typeDescriptor,
						OjbectPropertyOnReferencingType = propertyDescriptor,
						ColumnName = propertyDescriptor.PersistedName + relatedPropertyDescriptor.PersistedShortName,
						KeyPropertyOnForeignType = relatedPropertyDescriptor
					};

					i++;
				}

				return retval;
			}
			else
			{
				throw new InvalidOperationException();
			}
		}

		private string GetNextAlias()
		{
			return "T" + (aliasCount++);
		}

		private ProjectedColumns ProjectColumns(Expression expression, string newAlias, Dictionary<MemberInitExpression, SqlObjectOperand> memberInitBySqlObjectOperand, params string[] existingAliases)
		{
			return ColumnProjector.ProjectColumns(this.DataAccessModel, QueryBinder.CanBeColumn, expression, newAlias, memberInitBySqlObjectOperand, existingAliases);
		}

		private Expression BindContains(Expression checkList, Expression checkItem)
		{
			return new SqlFunctionCallExpression(typeof(bool), SqlFunction.In, Visit(checkItem), Visit(checkList));
		}

		private Expression BindFirst(Expression source, Expression defaultValueExpression, SelectFirstType selectFirstType)
		{
			var projection = this.VisitSequence(source);

			var select = projection.Select;

			var alias = this.GetNextAlias();

			var pc = ProjectColumns(projection.Projector, alias, this.objectOperandByMemberInit, projection.Select.Alias);

			int limit;

			switch (selectFirstType)
			{
				case SelectFirstType.FirstOrDefault:
					limit = 1;
					break;
				case SelectFirstType.Single:
					limit = 2;
					break;
				case SelectFirstType.SingleOrDefault:
					limit = 2;
					break;
				case SelectFirstType.DefaultIfEmpty:
					limit = 1;
					break;
				default:
					limit = 1;
					break;
			}

			return new SqlProjectionExpression(new SqlSelectExpression(select.Type, alias, pc.Columns, projection.Select, null, null, null, false, null, Expression.Constant(limit), select.ForUpdate), pc.Projector, null, false, selectFirstType, defaultValueExpression);
		}

		private Expression BindTake(Expression source, Expression take)
		{
			var projection = this.VisitSequence(source);

			take = this.Visit(take);

			var select = projection.Select;

			var alias = this.GetNextAlias();

			var pc = ProjectColumns(projection.Projector, alias, this.objectOperandByMemberInit, projection.Select.Alias);

			return new SqlProjectionExpression(new SqlSelectExpression(select.Type, alias, pc.Columns, projection.Select, null, null, null, false, null, take, select.ForUpdate), pc.Projector, null);
		}

		private Expression BindSkip(Expression source, Expression skip)
		{
			var projection = this.VisitSequence(source);

			skip = this.Visit(skip);

			var select = projection.Select;
			var alias = this.GetNextAlias();
			var pc = ProjectColumns(projection.Projector, alias, this.objectOperandByMemberInit, projection.Select.Alias);

			return new SqlProjectionExpression(new SqlSelectExpression(select.Type, alias, pc.Columns, projection.Select, null, null, null, false, skip, null, select.ForUpdate), pc.Projector, null);
		}

		protected override Expression VisitBinary(BinaryExpression binaryExpression)
		{
			if (this.isWithinClientSideCode)
			{
				return base.VisitBinary(binaryExpression);
			}

			Expression left, right;

			if (binaryExpression.Left.Type == typeof(string) && binaryExpression.Right.Type == typeof(string))
			{
				if (binaryExpression.NodeType == ExpressionType.Add)
				{
					left = Visit(binaryExpression.Left);
					right = Visit(binaryExpression.Right);

					return new SqlFunctionCallExpression(binaryExpression.Type, SqlFunction.Concat, left, right);
				}
			}

			if ((binaryExpression.NodeType == ExpressionType.GreaterThan
				|| binaryExpression.NodeType == ExpressionType.GreaterThanOrEqual
				|| binaryExpression.NodeType == ExpressionType.LessThan
				|| binaryExpression.NodeType == ExpressionType.LessThanOrEqual)
				&& binaryExpression.Left.NodeType == ExpressionType.Call)
			{
				var methodCallExpression = (MethodCallExpression)binaryExpression.Left;
				
				if (methodCallExpression.Method.Name == "CompareTo" && methodCallExpression.Arguments.Count == 1 && methodCallExpression.Method.ReturnType == typeof(int)
					&& binaryExpression.Right.NodeType == ExpressionType.Constant && ((ConstantExpression)binaryExpression.Right).Value.Equals(0))
				{
					return new SqlFunctionCallExpression(typeof(bool), SqlFunction.CompareObject, Expression.Constant(binaryExpression.NodeType), Visit(methodCallExpression.Object), Visit(methodCallExpression.Arguments[0]));
				}
			}

			if (binaryExpression.NodeType == ExpressionType.NotEqual
				|| binaryExpression.NodeType == ExpressionType.Equal)
			{
				var function = binaryExpression.NodeType == ExpressionType.NotEqual ? SqlFunction.IsNotNull : SqlFunction.IsNull;

				var leftConstantExpression = binaryExpression.Left as ConstantExpression;
				var rightConstantExpression = binaryExpression.Right as ConstantExpression;

				if (rightConstantExpression != null)
				{
					if (rightConstantExpression.Value == null)
					{
						if (leftConstantExpression == null || leftConstantExpression.Value != null)
						{
							return new SqlFunctionCallExpression(binaryExpression.Type, function, this.Visit(binaryExpression.Left));
						}
					}
				}

				if (leftConstantExpression != null)
				{
					if (leftConstantExpression.Value == null)
					{
						if (rightConstantExpression == null || rightConstantExpression.Value != null)
						{
							return new SqlFunctionCallExpression(binaryExpression.Type, function, this.Visit(binaryExpression.Right));
						}
					}
				}
			}

			if (binaryExpression.NodeType == ExpressionType.Coalesce)
            {
                left = Visit(binaryExpression.Left);
                right = Visit(binaryExpression.Right);

                return new SqlFunctionCallExpression(binaryExpression.Type, SqlFunction.Coalesce, new[] { left, right });
            }

			left = Visit(binaryExpression.Left);
			right = Visit(binaryExpression.Right);

			if (left.NodeType == ExpressionType.MemberInit)
			{
				left = objectOperandByMemberInit[(MemberInitExpression)left];
			}

			if (right.NodeType == ExpressionType.MemberInit)
			{
				right = objectOperandByMemberInit[(MemberInitExpression)right];
			}
			
			if (left.Type.IsEnum)
			{
				if (!right.Type.IsEnum)
				{
					right = Expression.Call(Expression.Call(null, MethodInfoFastRef.EnumToObjectMethod, Expression.Constant(left.Type), right), MethodInfoFastRef.ObjectToStringMethod);
					left = Expression.Call(left, MethodInfoFastRef.ObjectToStringMethod);
				}
			}
			else if (right.Type.IsEnum)
			{
				if (!left.Type.IsEnum)
				{
					left = Expression.Call(Expression.Call(null, MethodInfoFastRef.EnumToObjectMethod, Expression.Constant(right.Type), left), MethodInfoFastRef.ObjectToStringMethod);
					right = Expression.Call(right, MethodInfoFastRef.ObjectToStringMethod);
				}
			}

			var conversion = Visit(binaryExpression.Conversion);

			if (left != binaryExpression.Left || right != binaryExpression.Right || conversion != binaryExpression.Conversion)
			{
				if (binaryExpression.NodeType == ExpressionType.Coalesce)
				{
					return Expression.Coalesce(left, right, conversion as LambdaExpression);
				}

				if (left.NodeType == (ExpressionType)SqlExpressionType.ObjectOperand && right.NodeType == (ExpressionType)SqlExpressionType.Projection)
				{
					var objectOperandExpression = (SqlObjectOperand)left;
					var tupleExpression = new SqlTupleExpression(objectOperandExpression.ExpressionsInOrder);
					var selector = MakeSelectorForPrimaryKeys(left.Type, tupleExpression.Type);
					var rightWithSelect = BindSelectForPrimaryKeyProjection(tupleExpression.Type, (SqlProjectionExpression)right, selector, false);

					return Expression.MakeBinary(binaryExpression.NodeType, tupleExpression, rightWithSelect, binaryExpression.IsLiftedToNull, binaryExpression.Method);
				}
				else if (left.NodeType == (ExpressionType)SqlExpressionType.Projection && right.NodeType == (ExpressionType)SqlExpressionType.ObjectOperand)
				{
					var objectOperandExpression = (SqlObjectOperand)right;
					var tupleExpression = new SqlTupleExpression(objectOperandExpression.ExpressionsInOrder);
					var selector = MakeSelectorForPrimaryKeys(right.Type, tupleExpression.Type);
					var leftWithSelect = BindSelectForPrimaryKeyProjection(tupleExpression.Type, (SqlProjectionExpression)right, selector, false);

					return Expression.MakeBinary(binaryExpression.NodeType, leftWithSelect, tupleExpression, binaryExpression.IsLiftedToNull, binaryExpression.Method);
				}
				else
				{
					return Expression.MakeBinary(binaryExpression.NodeType, left, right, binaryExpression.IsLiftedToNull, binaryExpression.Method);
				}
			}

			return binaryExpression;
		}

		private LambdaExpression MakeSelectorForPrimaryKeys(Type objectType,  Type returnType)
		{
			var parameter = Expression.Parameter(objectType);
			var constructor = returnType.GetConstructor(Type.EmptyTypes);
			var newExpression = Expression.New(constructor);

			var bindings = new List<MemberBinding>();
			var typeDescriptor = this.DataAccessModel.GetTypeDescriptor(objectType);

			var itemNumber = 1;

			foreach (var property in typeDescriptor.PrimaryKeyProperties)
			{
				var itemProperty = returnType.GetProperty("Item" + itemNumber, BindingFlags.Instance | BindingFlags.Public);
				bindings.Add(Expression.Bind(itemProperty, Expression.Property(parameter, property.PropertyName)));

				itemNumber++;
			}

			var body = Expression.MemberInit(newExpression, bindings);

			return Expression.Lambda(body, parameter);
		}

		private Expression BindSelectForPrimaryKeyProjection(Type resultType, SqlProjectionExpression projection, LambdaExpression selector, bool forUpdate)
		{
			Expression expression;
			var oldIsWithinClientSideCode = this.isWithinClientSideCode;
			
			AddExpressionByParameter(selector.Parameters[0], projection.Projector);

			this.isWithinClientSideCode = true;

			try
			{
				expression = this.Visit(selector.Body);
			}
			finally
			{
				this.isWithinClientSideCode = oldIsWithinClientSideCode;
			}

			var alias = this.GetNextAlias();
			var pc = ProjectColumns(expression, alias, this.objectOperandByMemberInit, projection.Select.Alias);

			return new SqlProjectionExpression(new SqlSelectExpression(resultType, alias, pc.Columns, projection.Select, null, null, forUpdate || projection.Select.ForUpdate), pc.Projector, null);
		}

		protected virtual Expression BindJoin(Type resultType, Expression outerSource, Expression innerSource, LambdaExpression outerKey, LambdaExpression innerKey, LambdaExpression resultSelector)
		{
			var outerProjection = (SqlProjectionExpression)this.Visit(outerSource);
			var innerProjection = (SqlProjectionExpression)this.Visit(innerSource);

			AddExpressionByParameter(outerKey.Parameters[0], outerProjection.Projector);
			var outerKeyExpr = this.Visit(outerKey.Body);
			AddExpressionByParameter(innerKey.Parameters[0], innerProjection.Projector);
			var innerKeyExpression = this.Visit(innerKey.Body);

			if (outerKeyExpr.NodeType == ExpressionType.MemberInit)
			{
				outerKeyExpr = objectOperandByMemberInit[(MemberInitExpression)outerKeyExpr];
			}

			if (innerKeyExpression.NodeType == ExpressionType.MemberInit)
			{
				innerKeyExpression = objectOperandByMemberInit[(MemberInitExpression)innerKeyExpression];
			}

			AddExpressionByParameter(resultSelector.Parameters[0], outerProjection.Projector);
			AddExpressionByParameter(resultSelector.Parameters[1], innerProjection.Projector);

			var resultExpr = this.Visit(resultSelector.Body);

			SqlJoinType joinType;

			if (outerProjection.IsDefaultIfEmpty && innerProjection.IsDefaultIfEmpty)
			{
				joinType = SqlJoinType.OuterJoin;
			}
			else if (outerProjection.IsDefaultIfEmpty && !innerProjection.IsDefaultIfEmpty)
			{
				joinType = SqlJoinType.RightJoin;
			}
			else if (!outerProjection.IsDefaultIfEmpty && innerProjection.IsDefaultIfEmpty)
			{
				joinType = SqlJoinType.LeftJoin;
			}
			else
			{
				joinType = SqlJoinType.InnerJoin;
			}

			var join = new SqlJoinExpression(resultType, joinType, outerProjection.Select, innerProjection.Select, Expression.Equal(outerKeyExpr, innerKeyExpression));
			
			var alias = this.GetNextAlias();

			var projectedColumns = ProjectColumns(resultExpr, alias, this.objectOperandByMemberInit,  outerProjection.Select.Alias, innerProjection.Select.Alias);

			return new SqlProjectionExpression(new SqlSelectExpression(resultType, alias, projectedColumns.Columns, join, null, null, outerProjection.Select.ForUpdate || innerProjection.Select.ForUpdate), projectedColumns.Projector, null);
		}

		protected virtual Expression BindSelectMany(Type resultType, Expression source, LambdaExpression collectionSelector, LambdaExpression resultSelector)
		{
			SqlJoinType joinType;
			ProjectedColumns projectedColumns; 
			var projection = (SqlProjectionExpression)this.Visit(source);
			AddExpressionByParameter(collectionSelector.Parameters[0], projection.Projector);
			var selector = Evaluator.PartialEval(this.DataAccessModel, collectionSelector.Body);
			var collectionProjection = (SqlProjectionExpression)this.Visit(selector);
			
			if (IsTable(selector.Type))
			{
				joinType = SqlJoinType.CrossJoin;
			}
			else
			{
				throw new NotSupportedException();
			}

			var join = new SqlJoinExpression(resultType, joinType, projection.Select, collectionProjection.Select, null);

			var alias = this.GetNextAlias();
            
			if (resultSelector == null)
			{
				projectedColumns = ProjectColumns(collectionProjection.Projector, alias, this.objectOperandByMemberInit, projection.Select.Alias, collectionProjection.Select.Alias);
			}
			else
			{
				AddExpressionByParameter(resultSelector.Parameters[0], projection.Projector);
				AddExpressionByParameter(resultSelector.Parameters[1], collectionProjection.Projector);
				
				var resultExpression = this.Visit(resultSelector.Body);

				projectedColumns = ProjectColumns(resultExpression, alias, this.objectOperandByMemberInit, projection.Select.Alias, collectionProjection.Select.Alias);				
			}

			return new SqlProjectionExpression(new SqlSelectExpression(resultType, alias, projectedColumns.Columns, join, null, null, false), projectedColumns.Projector, null);
		}

		protected virtual Expression BindGroupBy(Expression source, LambdaExpression keySelector, LambdaExpression elementSelector, LambdaExpression resultSelector)
		{
			var projection = this.VisitSequence(source);

			AddExpressionByParameter(keySelector.Parameters[0], projection.Projector);
			
			var keyExpression = this.Visit(keySelector.Body);

			var elementExpression = projection.Projector;

			if (elementSelector != null)
			{
				AddExpressionByParameter(elementSelector.Parameters[0], projection.Projector);
				elementExpression = this.Visit(elementSelector.Body);
			}

			// Use ProjectColumns to get group-by expressions from key expression
			var keyProjection = ProjectColumns(keyExpression, projection.Select.Alias, this.objectOperandByMemberInit, projection.Select.Alias);
			var groupExprs = new[] { keyExpression };

			// Make duplicate of source query as basis of element subquery by visiting the source again
			var subqueryBasis = this.VisitSequence(source);

			// Recompute key columns for group expressions relative to subquery (need these for doing the correlation predicate)
			AddExpressionByParameter(keySelector.Parameters[0], subqueryBasis.Projector);
			var subqueryKey = this.Visit(keySelector.Body);

			// Ise same projection trick to get group by expressions based on subquery
			var subQueryProjectedColumns = ProjectColumns(subqueryKey, subqueryBasis.Select.Alias, this.objectOperandByMemberInit, subqueryBasis.Select.Alias);
			var subqueryGroupExprs = new[] { subqueryKey };// CHANGED TO ALLOW FUNCTION CALL GROUPBY subQueryProjectedColumns.Columns.Select(c => c.Expression);
			var subqueryCorrelation = BuildPredicateWithNullsEqual(subqueryGroupExprs, groupExprs);

			// Compute element based on duplicated subquery
			var subqueryElemExpr = subqueryBasis.Projector;

			if (elementSelector != null)
			{
				AddExpressionByParameter(elementSelector.Parameters[0], subqueryBasis.Projector);
				subqueryElemExpr = this.Visit(elementSelector.Body);
			}

			// Build subquery that projects the desired element

			var elementAlias = this.GetNextAlias();

			var elementProjectedColumns = ProjectColumns(subqueryElemExpr, elementAlias, this.objectOperandByMemberInit, subqueryBasis.Select.Alias);
			
			var elementSubquery = new SqlProjectionExpression
			(
				new SqlSelectExpression
				(
					TypeHelper.GetSequenceType(subqueryElemExpr.Type),
					elementAlias,
					elementProjectedColumns.Columns,
					subqueryBasis.Select,
					subqueryCorrelation,
					null,
					subqueryBasis.Select.ForUpdate
				),
				elementProjectedColumns.Projector,
				null
			);

			var alias = this.GetNextAlias();

			// Make it possible to tie aggregates back to this group by
			var info = new GroupByInfo(alias, elementExpression);

			this.groupByMap.Add(elementSubquery, info);

			Expression resultExpression;

			if (resultSelector != null)
			{
				var saveGroupElement = this.currentGroupElement;

				this.currentGroupElement = elementSubquery;

				// Compute result expression based on key & element-subquery
				AddExpressionByParameter(resultSelector.Parameters[0], keyProjection.Projector);
				AddExpressionByParameter(resultSelector.Parameters[1], elementSubquery);
				resultExpression = this.Visit(resultSelector.Body);

				this.currentGroupElement = saveGroupElement;
			}
			else
			{
				// Result must be IGrouping<K,E>
				resultExpression = Expression.New(typeof(Grouping<,>).MakeGenericType(keyExpression.Type, subqueryElemExpr.Type).GetConstructors()[0], new Expression[] { keyExpression, elementSubquery });
			}

			var pc = ProjectColumns(resultExpression, alias, this.objectOperandByMemberInit, projection.Select.Alias);

			// Make it possible to tie aggregates back to this Group By

			var projectedElementSubquery = ((NewExpression)pc.Projector).Arguments[1];

			this.groupByMap.Add(projectedElementSubquery, info);

			return new SqlProjectionExpression
			(
				new SqlSelectExpression
				(
					TypeHelper.GetSequenceType(resultExpression.Type),
					alias,
					pc.Columns,
					projection.Select,
					null,
					null,
					groupExprs,
					false, null, null, projection.Select.ForUpdate
				),
				pc.Projector,
				null
			);
		}

		private static Expression BuildPredicateWithNullsEqual(IEnumerable<Expression> source1, IEnumerable<Expression> source2)
		{
			var enumerator1 = source1.GetEnumerator();
			var enumerator2 = source2.GetEnumerator();

			Expression result = null;

			while (enumerator1.MoveNext() && enumerator2.MoveNext())
			{
				var compare = Expression.Or
				(
					Expression.And
					(
						new SqlFunctionCallExpression(typeof(bool),
						SqlFunction.IsNull, enumerator1.Current),
						new SqlFunctionCallExpression(typeof(bool), SqlFunction.IsNull, enumerator2.Current)
					),
					Expression.Equal(enumerator1.Current, enumerator2.Current)
				);

				result = (result == null) ? compare : Expression.And(result, compare);
			}

			return result;
		}

		protected override Expression VisitMethodCall(MethodCallExpression methodCallExpression)
		{
            if (methodCallExpression.Method.DeclaringType == typeof(Queryable)
				|| methodCallExpression.Method.DeclaringType == typeof(Enumerable)
				|| methodCallExpression.Method.DeclaringType == typeof(QueryableExtensions))
			{
				switch (methodCallExpression.Method.Name)
				{
					case "Where":
						return this.BindWhere(methodCallExpression.Type, methodCallExpression.Arguments[0], (LambdaExpression)StripQuotes(methodCallExpression.Arguments[1]), false);
					case "WhereForUpdate":
						return this.BindWhere(methodCallExpression.Type, methodCallExpression.Arguments[0], (LambdaExpression)StripQuotes(methodCallExpression.Arguments[1]), true);
					case "Select":
						return this.BindSelect(methodCallExpression.Type, methodCallExpression.Arguments[0], (LambdaExpression)StripQuotes(methodCallExpression.Arguments[1]), false);
					case "SelectForUpdate":
						return this.BindSelect(methodCallExpression.Type, methodCallExpression.Arguments[0], (LambdaExpression)StripQuotes(methodCallExpression.Arguments[1]), true);
					case "OrderBy":
						return this.BindOrderBy(methodCallExpression.Type, methodCallExpression.Arguments[0], (LambdaExpression)StripQuotes(methodCallExpression.Arguments[1]), OrderType.Ascending);
					case "OrderByDescending":
						return this.BindOrderBy(methodCallExpression.Type, methodCallExpression.Arguments[0], (LambdaExpression)StripQuotes(methodCallExpression.Arguments[1]), OrderType.Descending);
					case "ThenBy":
						return this.BindThenBy(methodCallExpression.Arguments[0], (LambdaExpression)StripQuotes(methodCallExpression.Arguments[1]), OrderType.Ascending);
					case "ThenByDescending":
						return this.BindThenBy(methodCallExpression.Arguments[0], (LambdaExpression)StripQuotes(methodCallExpression.Arguments[1]), OrderType.Descending);
					case "GroupBy":
						if (methodCallExpression.Arguments.Count == 2)
						{
							return this.BindGroupBy
							(
								methodCallExpression.Arguments[0],
								(LambdaExpression)StripQuotes(methodCallExpression.Arguments[1]),
								null,
								null
							);
						}
						else if (methodCallExpression.Arguments.Count == 3)
						{
							return this.BindGroupBy
							(
								methodCallExpression.Arguments[0],
								(LambdaExpression)StripQuotes(methodCallExpression.Arguments[1]),
								(LambdaExpression)StripQuotes(methodCallExpression.Arguments[2]),
								null
							);
						}
						else if (methodCallExpression.Arguments.Count == 4)
						{
							return this.BindGroupBy
							(
								methodCallExpression.Arguments[0],
								(LambdaExpression)StripQuotes(methodCallExpression.Arguments[1]),
								(LambdaExpression)StripQuotes(methodCallExpression.Arguments[2]),
								(LambdaExpression)StripQuotes(methodCallExpression.Arguments[3])
							);
						}
						break;
					case "Count":
					case "Min":
					case "Max":
					case "Sum":
					case "Average":
						if (methodCallExpression.Arguments.Count == 1)
						{
							return this.BindAggregate(methodCallExpression.Arguments[0], methodCallExpression.Method, null, methodCallExpression == this.rootExpression);
						}
						else if (methodCallExpression.Arguments.Count == 2)
						{
							var selector = (LambdaExpression)StripQuotes(methodCallExpression.Arguments[1]);

							return this.BindAggregate(methodCallExpression.Arguments[0], methodCallExpression.Method, selector, methodCallExpression == this.rootExpression);
						}
						break;
					case "Distinct":
						return this.BindDistinct(methodCallExpression.Type, methodCallExpression.Arguments[0]);
					case "Join":
						return this.BindJoin(methodCallExpression.Type, methodCallExpression.Arguments[0],
						                     methodCallExpression.Arguments[1],
						                     (LambdaExpression)StripQuotes(methodCallExpression.Arguments[2]),
						                     (LambdaExpression)StripQuotes(methodCallExpression.Arguments[3]),
						                     (LambdaExpression)StripQuotes(methodCallExpression.Arguments[4]));
					case "SelectMany":
						if (methodCallExpression.Arguments.Count == 2)
						{
							return this.BindSelectMany(methodCallExpression.Type, methodCallExpression.Arguments[0], (LambdaExpression)StripQuotes(methodCallExpression.Arguments[1]), null);
						}
						else if (methodCallExpression.Arguments.Count == 3)
						{
							return this.BindSelectMany(methodCallExpression.Type, methodCallExpression.Arguments[0], (LambdaExpression)StripQuotes(methodCallExpression.Arguments[1]), (LambdaExpression)StripQuotes(methodCallExpression.Arguments[2]));
						}
						break;
					case "Skip":
						if (methodCallExpression.Arguments.Count == 2)
						{
							return this.BindSkip(methodCallExpression.Arguments[0], methodCallExpression.Arguments[1]);
						}
						break;
					case "Take":
						if (methodCallExpression.Arguments.Count == 2)
						{
							return this.BindTake(methodCallExpression.Arguments[0], methodCallExpression.Arguments[1]);
						}
						break;
					case "First":
						if (methodCallExpression.Arguments.Count == 1)
						{
							var retval = this.BindFirst(methodCallExpression.Arguments[0], null, SelectFirstType.First);

							return retval;
						}
						else if (methodCallExpression.Arguments.Count == 2)
						{
							var where = Expression.Call(null, this.DataAccessModel.AssemblyBuildInfo.GetQueryableWhereMethod(methodCallExpression.Type), methodCallExpression.Arguments[0], (LambdaExpression)StripQuotes(methodCallExpression.Arguments[1]));

							return this.BindFirst(where, null, SelectFirstType.First);
						}
						break;
					case "FirstOrDefault":
						if (methodCallExpression.Arguments.Count == 1)
						{
							return this.BindFirst(methodCallExpression.Arguments[0], null, SelectFirstType.FirstOrDefault);
						}
						else if (methodCallExpression.Arguments.Count == 2)
						{
							var where = Expression.Call(null, this.DataAccessModel.AssemblyBuildInfo.GetQueryableWhereMethod(methodCallExpression.Type), methodCallExpression.Arguments[0], (LambdaExpression)StripQuotes(methodCallExpression.Arguments[1]));

							return this.BindFirst(where, null, SelectFirstType.FirstOrDefault);
						}
						break;
					case "Single":
						if (methodCallExpression.Arguments.Count == 1)
						{
							return this.BindFirst(methodCallExpression.Arguments[0], null, SelectFirstType.Single);
						}
						else if (methodCallExpression.Arguments.Count == 2)
						{
							var where = Expression.Call(null, this.DataAccessModel.AssemblyBuildInfo.GetQueryableWhereMethod(methodCallExpression.Type), methodCallExpression.Arguments[0], (LambdaExpression)StripQuotes(methodCallExpression.Arguments[1]));

							return this.BindFirst(where, null, SelectFirstType.Single);
						}
						break;
					case "SingleOrDefault":
						if (methodCallExpression.Arguments.Count == 1)
						{
							return this.BindFirst(methodCallExpression.Arguments[0], null, SelectFirstType.SingleOrDefault);
						}
						else if (methodCallExpression.Arguments.Count == 2)
						{
							var where = Expression.Call(null, this.DataAccessModel.AssemblyBuildInfo.GetQueryableWhereMethod(methodCallExpression.Type), methodCallExpression.Arguments[0], (LambdaExpression)StripQuotes(methodCallExpression.Arguments[1]));

							return this.BindFirst(where, null, SelectFirstType.SingleOrDefault);
						}
						break;
					case "DefaultIfEmpty":
						if (methodCallExpression.Arguments.Count == 1)
						{
							var projectionExpression = (SqlProjectionExpression)this.Visit(methodCallExpression.Arguments[0]);

							return projectionExpression.ToDefaultIfEmpty();
						}
						else if (methodCallExpression.Arguments.Count == 2)
						{
							return this.BindFirst(methodCallExpression.Arguments[0], methodCallExpression.Arguments[1], SelectFirstType.DefaultIfEmpty);
						}
						break;
					case "Contains":
						if (methodCallExpression.Arguments.Count == 2)
						{
							return this.BindContains(methodCallExpression.Arguments[0], methodCallExpression.Arguments[1]);
						}
						break;
				}

				throw new NotSupportedException(String.Format("Linq function \"{0}\" is not supported", methodCallExpression.Method.Name));
			}
			else if (methodCallExpression.Method.DeclaringType == typeof(DataAccessObjectsQueryableExtensions))
			{
				switch (methodCallExpression.Method.Name)
				{
					case "DeleteImmediately":
						if (methodCallExpression.Arguments.Count == 2)
						{
							return this.BindDelete(methodCallExpression.Arguments[0], (LambdaExpression)(StripQuotes(methodCallExpression.Arguments[1])));
						}
						break;
				}
			}
			else if (methodCallExpression.Method.DeclaringType == typeof(SqlDatabaseTransactionContext))
			{
				switch (methodCallExpression.Method.Name)
				{
					case "DeleteHelper":
						if (methodCallExpression.Arguments.Count == 2)
						{
							return this.BindDelete(methodCallExpression.Arguments[0], (LambdaExpression)(StripQuotes(methodCallExpression.Arguments[1])));
						}
						break;
				}
			}

			if (typeof(IList).IsAssignableFrom(methodCallExpression.Method.DeclaringType)
				|| typeof(ICollection).IsAssignableFrom(methodCallExpression.Method.DeclaringType)
				|| typeof(ICollection<>).IsAssignableFromIgnoreGenericParameters(methodCallExpression.Method.DeclaringType))
			{
				switch (methodCallExpression.Method.Name)
				{
					case "Contains":
						if (methodCallExpression.Arguments.Count == 1)
						{
							return this.BindContains(methodCallExpression.Object, methodCallExpression.Arguments[0]);
						}
						break;
				}
			}
			else if (!this.isWithinClientSideCode && methodCallExpression.Method == MethodInfoFastRef.StringExtensionsIsLikeMethodInfo)
			{
				var operand1 = Visit(methodCallExpression.Arguments[0]);
				var operand2 = Visit(methodCallExpression.Arguments[1]);

				return new SqlFunctionCallExpression(typeof(bool), SqlFunction.Like, operand1, operand2);
			}
			else if (methodCallExpression.Method.DeclaringType == typeof(string))
			{
				switch (methodCallExpression.Method.Name)
				{
					case "Contains":
					{
						var operand0 = Visit(methodCallExpression.Object);

						if (methodCallExpression.Arguments.Count == 1)
						{
							var operand1 = Visit(methodCallExpression.Arguments[0]);

							return new SqlFunctionCallExpression(methodCallExpression.Type, SqlFunction.ContainsString, operand0, operand1);
						}

						break;
					}
					case "StartsWith":
					{
						var operand0 = Visit(methodCallExpression.Object);
							
						if (methodCallExpression.Arguments.Count == 1)
						{
							var operand1 = Visit(methodCallExpression.Arguments[0]);

							return new SqlFunctionCallExpression(methodCallExpression.Type, SqlFunction.StartsWith, operand0, operand1);
						}

						break;
					}
					case "EndsWith":
					{
						var operand0 = Visit(methodCallExpression.Object);

						if (methodCallExpression.Arguments.Count == 1)
						{
							var operand1 = Visit(methodCallExpression.Arguments[0]);

							return new SqlFunctionCallExpression(methodCallExpression.Type, SqlFunction.EndsWith, operand0, operand1);
						}

						break;
					}
					case "Substring":
					{
						var operand0 = Visit(methodCallExpression.Object);
						var operand1 = Visit(methodCallExpression.Arguments[0]);
						var operand2 = Visit(methodCallExpression.Arguments[1]);

						if (methodCallExpression.Arguments.Count > 3)
						{
							var operand3 = Visit(methodCallExpression.Arguments[1]);

							return new SqlFunctionCallExpression(methodCallExpression.Type, SqlFunction.Substring, operand0, operand1,
								                                    operand2, operand3);
						}
						else
						{
							return new SqlFunctionCallExpression(methodCallExpression.Type, SqlFunction.Substring, operand0, operand1,
								                                    operand2);
						}
					}
					case "Trim":
					{
						var operand0 = Visit(methodCallExpression.Object);

						if (methodCallExpression.Arguments.Count == 1)
						{
							var newArrayExpression = methodCallExpression.Arguments[0] as NewArrayExpression;

							if (newArrayExpression == null || newArrayExpression.Expressions.Count > 0)
							{
								throw new NotSupportedException("String.Trim(char[])");
							}
						}

						return new SqlFunctionCallExpression(methodCallExpression.Type, SqlFunction.Trim, operand0);
					}
					case "TrimStart":
					{
						var operand0 = Visit(methodCallExpression.Object);

						if (methodCallExpression.Arguments.Count == 1)
						{
							var newArrayExpression = methodCallExpression.Arguments[0] as NewArrayExpression;
							var constantExpression = methodCallExpression.Arguments[0] as ConstantExpression;
							var constantPlaceholderExpression = methodCallExpression.Arguments[0] as SqlConstantPlaceholderExpression;

							if ((newArrayExpression == null || newArrayExpression.Expressions.Count > 0)
								&& constantExpression == null && constantPlaceholderExpression == null)
							{
								throw new NotSupportedException("String.TrimStart(char[])");
							}
						}

						return new SqlFunctionCallExpression(methodCallExpression.Type, SqlFunction.TrimLeft, operand0);
					}
					case "TrimEnd":
					{
						var operand0 = Visit(methodCallExpression.Object);

						if (methodCallExpression.Arguments.Count == 1)
						{
							var newArrayExpression = methodCallExpression.Arguments[0] as NewArrayExpression;
							var constantExpression = methodCallExpression.Arguments[0] as ConstantExpression;
							var constantPlaceholderExpression = methodCallExpression.Arguments[0] as SqlConstantPlaceholderExpression;

							if ((newArrayExpression == null || newArrayExpression.Expressions.Count > 0)
								&& constantExpression == null && constantPlaceholderExpression == null)
							{
								throw new NotSupportedException("String.TrimEnd(char[])");
							}
						}

						return new SqlFunctionCallExpression(methodCallExpression.Type, SqlFunction.TrimRight, operand0);
					}
					case "ToUpper":
					{
						var operand0 = Visit(methodCallExpression.Object);

						if (methodCallExpression.Arguments.Count != 0)
						{
							throw new NotSupportedException("String.Upper()");
						}

						return new SqlFunctionCallExpression(methodCallExpression.Type, SqlFunction.Upper, operand0);
						}
					case "ToLower":
					{
						var operand0 = Visit(methodCallExpression.Object);

						if (methodCallExpression.Arguments.Count != 0)
						{
							throw new NotSupportedException("String.Lower()");
						}

						return new SqlFunctionCallExpression(methodCallExpression.Type, SqlFunction.Lower, operand0);
					}
				}
			}
			else if (methodCallExpression.Method.ReturnType.IsDataAccessObjectType())
			{
				return CreateSqlObjectOperand(methodCallExpression);
			}

			return base.VisitMethodCall(methodCallExpression);
		}
        
		protected virtual Expression BindOrderBy(Type resultType, Expression source, LambdaExpression orderSelector, OrderType orderType)
		{
			var myThenBys = this.thenBys;
			
			this.thenBys = null;
			
			var orderings = new List<SqlOrderByExpression>();
			var projection = (SqlProjectionExpression)this.Visit(source);

			AddExpressionByParameter(orderSelector.Parameters[0], projection.Projector);
			orderings.Add(new SqlOrderByExpression(orderType, this.Visit(orderSelector.Body)));

			if (myThenBys != null)
			{
				for (var i = myThenBys.Count - 1; i >= 0; i--)
				{
					var thenBy = myThenBys[i];
					var lambda = (LambdaExpression)thenBy.Expression;

					AddExpressionByParameter(lambda.Parameters[0], projection.Projector);
					orderings.Add(new SqlOrderByExpression(thenBy.OrderType, this.Visit(lambda.Body)));
				}
			}

			var alias = this.GetNextAlias();
			var projectedColumns = ProjectColumns(projection.Projector, alias, this.objectOperandByMemberInit, projection.Select.Alias);
			
			return new SqlProjectionExpression(new SqlSelectExpression(resultType, alias, projectedColumns.Columns, projection.Select, null, orderings.AsReadOnly(), projection.Select.ForUpdate), projectedColumns.Projector, null);
		}

		protected virtual Expression BindThenBy(Expression source, LambdaExpression orderSelector, OrderType orderType)
		{
			if (this.thenBys == null)
			{
				this.thenBys = new List<SqlOrderByExpression>();
			}

			this.thenBys.Add(new SqlOrderByExpression(orderType, orderSelector));

			return this.Visit(source);
		}

		private Expression BindWhere(Type resultType, Expression source, LambdaExpression predicate, bool forUpdate)
		{
			var projection = (SqlProjectionExpression)this.Visit(source);

			AddExpressionByParameter(predicate.Parameters[0], projection.Projector);

			var where = this.Visit(predicate.Body);

			var alias = this.GetNextAlias();

			var pc = ProjectColumns(projection.Projector, alias, this.objectOperandByMemberInit, GetExistingAlias(projection.Select));

			return new SqlProjectionExpression(new SqlSelectExpression(resultType, alias, pc.Columns, projection.Select, where, null, forUpdate), pc.Projector, null);
		}

		private Expression BindDistinct(Type resultType, Expression source)
		{
			var projection = this.VisitSequence(source);
			var select = projection.Select;
			var alias = this.GetNextAlias();

			var projectedColumns = ProjectColumns(projection.Projector, alias, this.objectOperandByMemberInit, projection.Select.Alias);

			return new SqlProjectionExpression(new SqlSelectExpression(resultType, alias, projectedColumns.Columns, projection.Select, null, null, null, true, null, null, select.ForUpdate), projectedColumns.Projector, null);
		}

		private Expression currentGroupElement;

		private static SqlAggregateType GetAggregateType(string methodName)
		{
			switch (methodName)
			{
				case "Count":
					return SqlAggregateType.Count;
				case "Min":
					return SqlAggregateType.Min;
				case "Max":
					return SqlAggregateType.Max;
				case "Sum":
					return SqlAggregateType.Sum;
				case "Average":
					return SqlAggregateType.Average;
				default:
					throw new Exception(String.Concat("Unknown aggregate type: ", methodName));
			}
		}

		private static bool HasPredicateArg(SqlAggregateType aggregateType)
		{
			return aggregateType == SqlAggregateType.Count;
		}

		private SqlProjectionExpression VisitSequence(Expression source)
		{
			return ConvertToSequence(this.Visit(source));
		}

		private SqlProjectionExpression ConvertToSequence(Expression expression)
		{
			switch (expression.NodeType)
			{
				case (ExpressionType)SqlExpressionType.Projection:
					return (SqlProjectionExpression)expression;
				case ExpressionType.New:
					var newExpression = (NewExpression)expression;

					if (expression.Type.IsGenericType && expression.Type.GetGenericTypeDefinition() == typeof(Grouping<,>))
					{
						return (SqlProjectionExpression)newExpression.Arguments[1];
					}

					goto default;
				case ExpressionType.MemberAccess:
					var memberAccessExpression = (MemberExpression)expression;

					if (expression.Type.IsGenericType && expression.Type.GetGenericTypeDefinition() == TypeHelper.RelatedDataAccessObjectsType)
					{
						var typeDescriptor = this.DataAccessModel.GetTypeDescriptor(expression.Type.GetGenericArguments()[0]);
						var parentTypeDescriptor = this.DataAccessModel.GetTypeDescriptor(memberAccessExpression.Expression.Type);
						var source = Expression.Constant(null, this.DataAccessModel.AssemblyBuildInfo.GetDataAccessObjectsType(typeDescriptor.Type));
						var concreteType = this.DataAccessModel.GetConcreteTypeFromDefinitionType(typeDescriptor.Type);
						var parameter = Expression.Parameter(typeDescriptor.Type, "relatedObject");
						PropertyDescriptor relatedProperty = typeDescriptor.GetRelatedProperty(parentTypeDescriptor.Type);

						var relatedPropertyName = relatedProperty.PersistedName;

						var body = Expression.Equal
						(
							Expression.Property(parameter, relatedProperty.PropertyInfo),
							memberAccessExpression.Expression
						);

						var condition = Expression.Lambda(body, parameter);

						return (SqlProjectionExpression)BindWhere(expression.Type.GetGenericArguments()[0], source, condition, false);
					}
					else if (expression.Type.IsGenericType && expression.Type.GetGenericTypeDefinition() == TypeHelper.IQueryableType)
					{
						if (memberAccessExpression.Expression.NodeType == ExpressionType.Constant)
						{
							return null;
						}

						var elementType = TypeHelper.GetElementType(expression.Type);

						return GetTableProjection(elementType);
					}
					goto default;
				default:
					throw new Exception(string.Format("The expression of type '{0}' is not a sequence", expression.Type));
			}
		}

		private Expression BindAggregate(Expression source, MethodInfo method, LambdaExpression argument, bool isRoot)
		{
			var isDistinct = false;
			var argumentWasPredicate = false; 
			var returnType = method.ReturnType;
			var aggregateType = GetAggregateType(method.Name);
			var hasPredicateArg = HasPredicateArg(aggregateType);

			// Check for distinct
			var methodCallExpression = source as MethodCallExpression;
			
			if (methodCallExpression != null && !hasPredicateArg && argument == null)
			{
				if (methodCallExpression.Method.Name == "Distinct"
					&& methodCallExpression.Arguments.Count == 1
					&& (methodCallExpression.Method.DeclaringType == typeof(Queryable) || methodCallExpression.Method.DeclaringType == typeof(Enumerable)))
				{
					source = methodCallExpression.Arguments[0];

					isDistinct = true;
				}
			}

			if (argument != null && hasPredicateArg)
			{
				// Convert Query.Count(predicate) into Query.Where(predicate).Count()

				source = Expression.Call(typeof(Queryable), "Where", method.GetGenericArguments(), source, argument);
				argument = null;
				argumentWasPredicate = true;
			}

			var projection = this.VisitSequence(source);

			Expression argumentExpression = null;

			if (argument != null)
			{
				AddExpressionByParameter(argument.Parameters[0], projection.Projector);

				argumentExpression = this.Visit(argument.Body);
			}
			else if (!hasPredicateArg)
			{
				argumentExpression = projection.Projector;
			}

			var alias = this.GetNextAlias();

			var projectedColumns = ProjectColumns(projection.Projector, alias, this.objectOperandByMemberInit, projection.Select.Alias);
			var aggregateExpression = new SqlAggregateExpression(returnType, aggregateType, argumentExpression, isDistinct);
			var selectType = this.DataAccessModel.AssemblyBuildInfo.GetEnumerableType(returnType);

			var select = new SqlSelectExpression
			(
				selectType,
				alias,
				new [] { new SqlColumnDeclaration("", aggregateExpression) },
				projection.Select,
				null,
				null,
                projection.Select.ForUpdate
			);

			if (isRoot)
			{
				var parameterExpression = Expression.Parameter(selectType, "PARAM");

				var aggregator = Expression.Lambda(Expression.Call(typeof(Enumerable), "Single", new Type[] { returnType }, parameterExpression), parameterExpression);

				return new SqlProjectionExpression(select, new SqlColumnExpression(returnType, alias, ""), aggregator);
			}

			var subquery = new SqlSubqueryExpression(returnType, select);

			// If we can find the corresponding group info then we can build a n
			// AggregateSubquery node that will enable us to optimize the aggregate
			// expression later using AggregateRewriter

			GroupByInfo info;

			if (!argumentWasPredicate && this.groupByMap.TryGetValue(projection, out info))
			{
				// Use the element expression from the group by info to rebind the
				// argument so the resulting expression is one that would be legal 
				// to add to the columns in the select expression that has the corresponding 
				// group-by clause.

				if (argument != null)
				{
					AddExpressionByParameter(argument.Parameters[0], info.Element);

					argumentExpression = this.Visit(argument.Body);
				}
				else if (!hasPredicateArg)
				{
					argumentExpression = info.Element;
				}

				aggregateExpression = new SqlAggregateExpression(returnType, aggregateType, argumentExpression, isDistinct);

				// Check for easy to optimize case.
				// If the projection that our aggregate is based on is really the 'group' argument from
				// the Query.GroupBy(xxx, (key, group) => yyy) method then whatever expression we return
				// here will automatically become part of the select expression that has the group-by
				// clause, so just return the simple aggregate expression.

				if (projection == this.currentGroupElement)
				{
					return aggregateExpression;
				}

				return new SqlAggregateSubqueryExpression(info.Alias, aggregateExpression, subquery);
			}

			return subquery;
		}

		private Expression BindSelect(Type resultType, Expression source, LambdaExpression selector, bool forUpdate)
		{
			Expression expression;
			var oldIsWithinClientSideCode = this.isWithinClientSideCode;
			var projection = (SqlProjectionExpression)this.Visit(source);

			AddExpressionByParameter(selector.Parameters[0], projection.Projector);

			this.isWithinClientSideCode = true;

			try
			{	
				expression = this.Visit(selector.Body);
			}
			finally
			{
				this.isWithinClientSideCode = oldIsWithinClientSideCode;
			}

			var alias = this.GetNextAlias();
			var pc = ProjectColumns(expression, alias, this.objectOperandByMemberInit, projection.Select.Alias);

			return new SqlProjectionExpression(new SqlSelectExpression(resultType, alias, pc.Columns, projection.Select, null, null, forUpdate || projection.Select.ForUpdate), pc.Projector, null);
		}

		private Expression BindDelete(Expression source, LambdaExpression selector)
		{
			var localExtraCondition = this.extraCondition;

			this.extraCondition = null;

			var projection = this.GetTableProjection(((ConstantExpression)source).Type);

			AddExpressionByParameter(selector.Parameters[0], projection.Projector);
			
			if (localExtraCondition != null)
			{
				AddExpressionByParameter(localExtraCondition.Parameters[0], projection.Projector);
			}

			var expression = this.Visit(selector.Body);
			
			var tableExpression = ((SqlTableExpression)projection.Select.From);
            
			if (localExtraCondition != null)
			{
				expression = Expression.AndAlso(selector.Body, localExtraCondition.Body);
				expression = Visit(expression);
			}

			return new SqlDeleteExpression(tableExpression.Name, projection.Select.Alias, expression);
		}
        
		private static string GetExistingAlias(Expression source)
		{
			switch ((SqlExpressionType)source.NodeType)
			{
				case SqlExpressionType.Select:
					return ((SqlSelectExpression)source).Alias;
				case SqlExpressionType.Table:
					return ((SqlTableExpression)source).Alias;
				default:
					throw new InvalidOperationException(String.Concat("Invalid source node type: ", source.NodeType));
			}
		}

		private class MemberInitEqualityComparer
			: IEqualityComparer<MemberInitExpression>
		{
			public static readonly MemberInitEqualityComparer Default = new MemberInitEqualityComparer();

			public bool Equals(MemberInitExpression x, MemberInitExpression y)
			{
				return x.Type == y.Type && x.Bindings.Count == y.Bindings.Count;
			}

			public int GetHashCode(MemberInitExpression obj)
			{
				return obj.Type.GetHashCode() ^ obj.Bindings.Count;
			}
		}

		private readonly Dictionary<MemberInitExpression, SqlObjectOperand> objectOperandByMemberInit = new Dictionary<MemberInitExpression, SqlObjectOperand>(MemberInitEqualityComparer.Default);

		private SqlProjectionExpression GetTableProjection(Type type)
		{
			Type elementType;
			TypeDescriptor typeDescriptor;

			if (type.IsDataAccessObjectType())
			{
				typeDescriptor = this.typeDescriptorProvider.GetTypeDescriptor(type);

				elementType = typeDescriptor.Type;
			}
			else
			{
				typeDescriptor = this.typeDescriptorProvider.GetTypeDescriptor(TypeHelper.GetElementType(type));

				elementType = typeDescriptor.Type;
			}

			var tableAlias = this.GetNextAlias();
			var selectAlias = this.GetNextAlias();

			var bindings = new List<MemberBinding>();
			var columns = new List<SqlColumnDeclaration>();

			foreach (var propertyDescriptor in typeDescriptor.PersistedProperties.Filter(c => !c.PropertyType.IsDataAccessObjectType()))
			{
				var ordinal = columns.Count;
				var columnName = propertyDescriptor.PersistedName;
				var columnType = propertyDescriptor.PropertyType;
				var propertyInfo = propertyDescriptor.PropertyInfo;

				bindings.Add(Expression.Bind(propertyInfo, new SqlColumnExpression(columnType, selectAlias, columnName)));
				columns.Add(new SqlColumnDeclaration(columnName, new SqlColumnExpression(columnType, tableAlias, columnName)));
			}

			foreach (var propertyDescriptor in typeDescriptor.RelatedProperties.Filter(c => c.IsBackReferenceProperty).Concat(typeDescriptor.PersistedProperties.Filter(c => c.PropertyType.IsDataAccessObjectType())))
			{
				var columnType = propertyDescriptor.PropertyType;
				var columnExpressions = new List<Expression>();
				var propertyNames = new List<string>();
				
				foreach (var v in QueryBinder.ExpandPropertyIntoForeignKeyColumns(this.DataAccessModel, propertyDescriptor))
				{
					var expression = new SqlColumnExpression(v.KeyPropertyOnForeignType.PropertyType, selectAlias, v.ColumnName);

					columnExpressions.Add(expression);
					propertyNames.Add(v.KeyPropertyOnForeignType.PropertyName);
					columns.Add(new SqlColumnDeclaration(v.ColumnName, new SqlColumnExpression(v.KeyPropertyOnForeignType.PropertyType, tableAlias, v.ColumnName)));
				}

				var objectOperand = new SqlObjectOperand(columnType, columnExpressions, propertyNames);

				bindings.Add(Expression.Bind(propertyDescriptor.PropertyInfo, objectOperand));
			}

			var primaryKeyPropertyNames = new List<string>();
			var primaryKeyColumnExpressions = new List<Expression>();
			SqlObjectOperand primaryKeyObjectOperand = null;

			foreach (var propertyDescriptor in typeDescriptor.PrimaryKeyProperties)
			{
				var expression = new SqlColumnExpression(propertyDescriptor.PropertyType, selectAlias, propertyDescriptor.PersistedName);

				primaryKeyPropertyNames.Add(propertyDescriptor.PropertyName);
				primaryKeyColumnExpressions.Add(expression);
			}

			primaryKeyObjectOperand = new SqlObjectOperand(typeDescriptor.Type, primaryKeyColumnExpressions, primaryKeyPropertyNames);

			var projectorExpression = Expression.MemberInit(Expression.New(elementType), bindings);
			objectOperandByMemberInit[projectorExpression] = primaryKeyObjectOperand;

			var resultType = this.DataAccessModel.AssemblyBuildInfo.GetEnumerableType(elementType);
			var projection = new SqlProjectionExpression(new SqlSelectExpression(resultType, selectAlias, columns, new SqlTableExpression(resultType, tableAlias, typeDescriptor.PersistedName), null, null, false), projectorExpression, null);

			if ((conditionType == elementType || (conditionType != null && conditionType.IsAssignableFrom(elementType))) && extraCondition != null)
			{
				AddExpressionByParameter(extraCondition.Parameters[0], projection.Projector);

				var where = this.Visit(this.extraCondition.Body);
				var alias = this.GetNextAlias();
				var pc = ProjectColumns(projection.Projector, alias, this.objectOperandByMemberInit, GetExistingAlias(projection.Select));

				return new SqlProjectionExpression(new SqlSelectExpression(resultType, alias, pc.Columns, projection.Select, where, null, false), pc.Projector, null);
			}

			return projection;
		}

		protected static bool IsTable(Type type)
		{
			if (type.IsGenericType)
			{
				var genericType = type.GetGenericTypeDefinition();

				return genericType == TypeHelper.DataAccessObjectsType || genericType == TypeHelper.RelatedDataAccessObjectsType || genericType == TypeHelper.IQueryableType && type.GetGenericArguments()[0].IsDataAccessObjectType();
			}

			return false;
		}

		protected override Expression VisitConstant(ConstantExpression constantExpression)
		{
			if (IsTable(constantExpression.Type))
			{
				return GetTableProjection(constantExpression.Type);
			}
			else if (constantExpression.Type.IsDataAccessObjectType() && !this.isWithinClientSideCode)
			{
				return CreateSqlObjectOperand(constantExpression);
			}

			return constantExpression;
		}
        
		protected override Expression VisitParameter(ParameterExpression p)
		{
			Expression e;

			if (this.expressionsByParameter.TryGetValue(p, out e))
			{
				return e;
			}

			return p;
		}

		protected Expression CreateSqlObjectOperand(Expression expression)
		{
			var type = expression.Type;
			var propertyNames = new List<string>();
			var columnExpressions = new List<Expression>();
			var typeDescriptor = this.DataAccessModel.GetTypeDescriptor(type);

			foreach (var primaryKey in typeDescriptor.PrimaryKeyProperties)
			{
				var newExpression = Expression.Property(expression, primaryKey.PropertyInfo);

				propertyNames.Add(primaryKey.PropertyName);
				columnExpressions.Add(newExpression);
			}

			return new SqlObjectOperand(type, columnExpressions, propertyNames);
		}

		protected override Expression VisitMemberAccess(MemberExpression memberExpression)
		{
			var source = this.Visit(memberExpression.Expression);

			if (source == null)
			{
				return MakeMemberAccess(null, memberExpression.Member);
			}

			switch (source.NodeType)
			{
				case ExpressionType.MemberInit:
					var min = (MemberInitExpression)source;

					if (min.Bindings != null)
					{
						for (int i = 0, n = min.Bindings.Count; i < n; i++)
						{
							var assign = min.Bindings[i] as MemberAssignment;

							if (assign != null && MembersMatch(assign.Member, memberExpression.Member))
							{
								return assign.Expression;
							}
						}
					}

					break;
				case ExpressionType.New:
					// Source is a anonymous type from a join
					var newExpression = (NewExpression)source;

					if (newExpression.Members != null)
					{
						for (int i = 0, n = newExpression.Members.Count; i < n; i++)
						{
							if (MembersMatch(newExpression.Members[i], memberExpression.Member))
							{
								return newExpression.Arguments[i];
							}
						}
					}
				break;
				case ExpressionType.Constant:

					if (memberExpression.Type.IsDataAccessObjectType())
					{
						return CreateSqlObjectOperand(memberExpression);
					}
					else if (IsTable(memberExpression.Type))
					{
						return GetTableProjection(memberExpression.Type);
					}
					else
					{
						return memberExpression;
					}
			}
            
			if (source == memberExpression.Expression)
			{
				return memberExpression;
			}

			return MakeMemberAccess(source, memberExpression.Member);
		}

		private static bool MemberInfosMostlyMatch(MemberInfo a, MemberInfo b)
		{
			if (a == b)
			{
				return true;
			}

			if (a.GetType() == b.GetType())
			{
				if (a.Name == b.Name
					&& ((a.DeclaringType == b.DeclaringType) || a.DeclaringType.IsAssignableFrom(b.DeclaringType) || b.DeclaringType.IsAssignableFrom(a.DeclaringType)))
				{
					return true;
				}
			}

			return false;
		}

		private static bool MembersMatch(MemberInfo a, MemberInfo b)
		{
			if (a == b)
			{
				return true;
			}

			if (MemberInfosMostlyMatch(a, b))
			{
				return true;
			}

			if (a is MethodInfo && b is PropertyInfo)
			{
				return MemberInfosMostlyMatch(a, ((PropertyInfo)b).GetGetMethod());
			}
			else if (a is PropertyInfo && b is MethodInfo)
			{
				return MemberInfosMostlyMatch(((PropertyInfo)a).GetGetMethod(), b);
			}

			return false;
		}

		protected override Expression VisitUnary(UnaryExpression unaryExpression)
		{
			if (unaryExpression.NodeType == ExpressionType.Not)
			{
				if (unaryExpression.Operand.NodeType == ExpressionType.Call)
				{
					if (((MethodCallExpression)unaryExpression.Operand).Method == typeof(ShaolinqStringExtensions).GetMethod("IsLike",BindingFlags.Static | BindingFlags.Public))
					{
						var methodCallExpression = (MethodCallExpression)unaryExpression.Operand;

						var operand1 = Visit(methodCallExpression.Arguments[0]);
						var operand2 = Visit(methodCallExpression.Arguments[1]);
                        
						return new SqlFunctionCallExpression(typeof(bool), SqlFunction.NotLike, operand1, operand2);
					}
				}
			}
			else if (unaryExpression.NodeType == ExpressionType.Convert)
			{
				if (unaryExpression.Operand.Type.IsEnum && unaryExpression.Operand.Type != typeof(DayOfWeek))
				{
					return Visit(unaryExpression.Operand);	
				}
			}

			return base.VisitUnary(unaryExpression);
		}

		private Expression MakeMemberAccess(Expression source, MemberInfo memberInfo)
		{
			var fieldInfo = memberInfo as FieldInfo;

			if (typeof(ICollection<>).IsAssignableFromIgnoreGenericParameters(memberInfo.DeclaringType) && !this.isWithinClientSideCode)
			{
				if (memberInfo.Name == "Count")
				{
					return new SqlFunctionCallExpression(memberInfo.GetMemberReturnType(), SqlFunction.CollectionCount, source);
				}
			}
			else if (memberInfo.DeclaringType == typeof(DateTime) && !this.isWithinClientSideCode)
			{
				switch (memberInfo.Name)
				{
					case "Week":
						return new SqlFunctionCallExpression(memberInfo.GetMemberReturnType(), SqlFunction.Week, source);
					case "Month":
						return new SqlFunctionCallExpression(memberInfo.GetMemberReturnType(), SqlFunction.Month, source);
					case "Year":
						return new SqlFunctionCallExpression(memberInfo.GetMemberReturnType(), SqlFunction.Year, source);
					case "Hour":
						return new SqlFunctionCallExpression(memberInfo.GetMemberReturnType(), SqlFunction.Hour, source);
					case "Minute":
						return new SqlFunctionCallExpression(memberInfo.GetMemberReturnType(), SqlFunction.Minute, source);
					case "Second":
						return new SqlFunctionCallExpression(memberInfo.GetMemberReturnType(), SqlFunction.Second, source);
					case "DayOfWeek":
						return new SqlFunctionCallExpression(memberInfo.GetMemberReturnType(), SqlFunction.DayOfWeek, source);
					case "Day":
						return new SqlFunctionCallExpression(memberInfo.GetMemberReturnType(), SqlFunction.DayOfMonth, source);
					case "DayOfYear":
						return new SqlFunctionCallExpression(memberInfo.GetMemberReturnType(), SqlFunction.DayOfYear, source);
					case "Date":
						return new SqlFunctionCallExpression(memberInfo.GetMemberReturnType(), SqlFunction.Date, source);
					default:
						throw new NotSupportedException("Member access on DateTime: " + memberInfo);
				}
			}
			else if (memberInfo.DeclaringType == typeof(ServerDateTime))
			{
				switch (memberInfo.Name)
				{
					case "Now":
						return new SqlFunctionCallExpression(memberInfo.GetMemberReturnType(), SqlFunction.ServerDateTime);
				}
			}
			else if (typeof(IGrouping<,>).IsAssignableFromIgnoreGenericParameters(memberInfo.DeclaringType) && source is NewExpression && memberInfo.Name == "Key")
			{
				var newExpression = source as NewExpression;

				var arg = newExpression.Arguments[0];

				return arg;
			}
			else if (source != null && source.NodeType == (ExpressionType)SqlExpressionType.ObjectOperand)
			{
				Expression expression;
				var objectOperandExpression = (SqlObjectOperand)source;

				if (objectOperandExpression.ExpressionsByPropertyName.TryGetValue(memberInfo.Name, out expression))
				{
					return expression;
				}
			}

			if (fieldInfo != null)
			{
				return Expression.Field(source, fieldInfo);
			}

			var propertyInfo = memberInfo as PropertyInfo;

			if (propertyInfo != null)
			{
				// TODO: Throw an unsupported exception if not binding for SQL ToString implementation

				return Expression.Property(source, propertyInfo);
			}

			throw new NotSupportedException("MemberInfo: " + memberInfo);
		}

		internal static string GetKey(Dictionary<string, Expression> dictionary, Expression expression)
		{
			foreach (var keyValue in dictionary)
			{
				if (keyValue.Value == expression)
				{
					return keyValue.Key;
				}
			}

			throw new InvalidOperationException();
		}

		protected override Expression Visit(Expression expression)
		{
			if (expression == null)
			{
				return null;
			}

			switch (expression.NodeType)
			{
				case (ExpressionType)SqlExpressionType.ConstantPlaceholder:

					var result = Visit(((SqlConstantPlaceholderExpression) expression).ConstantExpression);

					if (!(result is ConstantExpression))
					{
						return result;
					}

					return expression;
				case (ExpressionType)SqlExpressionType.Column:
					return expression;
				case (ExpressionType)SqlExpressionType.ObjectOperand:
					SqlObjectOperand operand;
					List<Expression> newExpressions = null;
					List<string> newPropertyNames = null;

					operand = (SqlObjectOperand)expression;

					for (int i = 0; i < operand.ExpressionsInOrder.Count; i++)
					{
						var newoe = Visit(operand.ExpressionsInOrder[i]);
						
						if (newoe != operand.ExpressionsInOrder[i])
						{
							if (newExpressions == null)
							{
								newExpressions = new List<Expression>();
								newPropertyNames = new List<string>();
								
								for (int j = 0; j < i; j++)
								{
									newExpressions.Add(operand.ExpressionsInOrder[j]);
									var key = operand.PropertyNamesByExpression[operand.ExpressionsInOrder[j]];
									newPropertyNames.Add(key);
								}

								newExpressions.Add(newoe);
								var key2 = operand.PropertyNamesByExpression[operand.ExpressionsInOrder[i]];
								newPropertyNames.Add(key2);
							}
							else
							{
								newExpressions.Add(newoe);
								var key = operand.PropertyNamesByExpression[operand.ExpressionsInOrder[i]];
								newPropertyNames.Add(key);
							}
						}
					}

					if (newExpressions != null)
					{
						return new SqlObjectOperand(expression.Type, newExpressions, newPropertyNames);
					}
					else
					{
						return expression;
					}
			}

			return base.Visit(expression);
		}
	}
}
