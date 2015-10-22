// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Platform;
using Platform.Reflection;
using Shaolinq.Persistence.Linq.Expressions;
using Shaolinq.Persistence.Linq.Optimizers;
using PropertyPath = Shaolinq.Persistence.Linq.ObjectPath<System.Reflection.PropertyInfo>;

namespace Shaolinq.Persistence.Linq
{
	public class RelatedPropertiesJoinExpanderResults
	{
		public Expression ProcessedExpression { get; set; }
		public Dictionary<Expression, List<IncludedPropertyInfo>> IncludedPropertyInfos { get; set; }
		private readonly List<Pair<Expression, Dictionary<PropertyPath, Expression>>> replacementExpressionForPropertyPathsByJoin;

		internal RelatedPropertiesJoinExpanderResults(List<Pair<Expression, Dictionary<PropertyPath, Expression>>> replacementExpressionForPropertyPathsByJoin)
		{
			this.replacementExpressionForPropertyPathsByJoin = replacementExpressionForPropertyPathsByJoin;
		}

		public Expression GetReplacementExpression(Expression currentJoin, PropertyPath propertyPath)
		{
			int index;
			var indexFound = -1;

			for (index = this.replacementExpressionForPropertyPathsByJoin.Count - 1; index >= 0; index--)
			{
				Expression retval;

				if (currentJoin == this.replacementExpressionForPropertyPathsByJoin[index].Left)
				{
					indexFound = index;
				}

				if (index > indexFound)
				{
					continue;
				}
				
				if (this.replacementExpressionForPropertyPathsByJoin[index].Right.TryGetValue(propertyPath, out retval))
				{
					return retval;	
				}
			}

			for (index = this.replacementExpressionForPropertyPathsByJoin.Count - 1; index >= 0; index--)
			{
				Expression retval;

				if (currentJoin == this.replacementExpressionForPropertyPathsByJoin[index].Left)
				{
					indexFound = index;
				}

				if (index > indexFound)
				{
					continue;
				}

				if (this.replacementExpressionForPropertyPathsByJoin[index].Right.TryGetValue(propertyPath, out retval))
				{
					return retval;
				}

				if (this.replacementExpressionForPropertyPathsByJoin[index].Right.TryGetValue(PropertyPath.Empty, out retval))
				{
					foreach (var property in propertyPath)
					{
						retval = Expression.Property(retval, property.Name);
					}

					return retval;
				}
			}

			throw new InvalidOperationException();
		}
	}

	public class RelatedPropertiesJoinExpander
		: SqlExpressionVisitor
	{
		private readonly DataAccessModel model;
		private readonly Dictionary<Expression, List<IncludedPropertyInfo>> includedPropertyInfos = new Dictionary<Expression, List<IncludedPropertyInfo>>();
		private readonly List<Pair<Expression, Dictionary<PropertyPath, Expression>>> replacementExpressionForPropertyPathsByJoin = new List<Pair<Expression, Dictionary<PropertyPath, Expression>>>();

		private RelatedPropertiesJoinExpander(DataAccessModel model)
		{
			this.model = model;
		}

		public static RelatedPropertiesJoinExpanderResults Expand(DataAccessModel model, Expression expression)
		{
			expression = JoinSelectorExpander.Expand(expression);

			var visitor = new RelatedPropertiesJoinExpander(model);

			var processedExpression = visitor.Visit(expression);

			return new RelatedPropertiesJoinExpanderResults(visitor.replacementExpressionForPropertyPathsByJoin)
			{
				ProcessedExpression = processedExpression,
				IncludedPropertyInfos = visitor.includedPropertyInfos
			};
		}

		protected override Expression VisitMethodCall(MethodCallExpression methodCallExpression)
		{
			if (methodCallExpression.Method.DeclaringType != typeof(Queryable)
				&& methodCallExpression.Method.DeclaringType != typeof(Enumerable)
				&& methodCallExpression.Method.DeclaringType != typeof(QueryableExtensions))
			{
				return base.VisitMethodCall(methodCallExpression);
			}

			switch (methodCallExpression.Method.Name)
			{
			case "Where":
			case "WhereForUpdate":
				return this.RewriteBasicProjection(methodCallExpression, false);
			case "Select":
			case "SelectForUpdate":
				return this.RewriteBasicProjection(methodCallExpression, true);
			case "OrderBy":
				return this.RewriteBasicProjection(methodCallExpression, false);
			case "GroupBy":
				return this.RewriteBasicProjection(methodCallExpression, false);
			default:
				return base.VisitMethodCall(methodCallExpression);
			}
		}

		private static LambdaExpression MakeJoinProjector(Type leftType, Type rightType)
		{
			var leftParameter = Expression.Parameter(leftType);
			var rightParameter = Expression.Parameter(rightType);
			var newExpression = Expression.New(typeof(LeftRightJoinInfo<,>).MakeGenericType(leftType, rightType));

			var body = Expression.MemberInit(newExpression, 
				Expression.Bind(newExpression.Type.GetProperty("Left", BindingFlags.Public | BindingFlags.Instance), leftParameter),
				Expression.Bind(newExpression.Type.GetProperty("Right", BindingFlags.Public | BindingFlags.Instance), rightParameter));

			return Expression.Lambda(body, leftParameter, rightParameter);
		}

		private static MethodCallExpression MakeJoinCallExpression(int index, Expression left, Expression right, PropertyPath targetPath, Dictionary<PropertyPath, int> indexByPath, Dictionary<PropertyPath, Expression> rootExpressionsByPath, Expression sourceParameterExpression)
		{
			Expression leftObject;

			var leftElementType = left.Type.GetGenericArguments()[0];
			var rightElementType = right.Type.GetGenericArguments()[0];

			var rootPath = targetPath.PathWithoutLast();
			var leftSelectorParameter = Expression.Parameter(leftElementType);

			if (index == 1 && rootExpressionsByPath.ContainsKey(rootPath))
			{
				leftObject = rootExpressionsByPath[rootPath];

				leftObject = ExpressionReplacer.Replace(leftObject, c =>
				{
					if (c == sourceParameterExpression)
					{
						return leftSelectorParameter;
					}

					return null;
				});
			}
			else 
			{
				leftObject = CreateExpressionForPath(index - 1, rootPath, leftSelectorParameter, indexByPath);

				if (rootExpressionsByPath.ContainsKey(rootPath))
				{
					foreach (var property in rootPath)
					{
						leftObject = Expression.Property(leftObject, property.Name);
					}
				}
			}

			var leftSelector = Expression.Lambda(Expression.Property(leftObject, targetPath.Last().Name), leftSelectorParameter);

			var rightSelectorParameter = Expression.Parameter(rightElementType);
			var rightSelector = Expression.Lambda(rightSelectorParameter, rightSelectorParameter);

			var projector = MakeJoinProjector(leftElementType, rightElementType);

			right = Expression.Call(null, MethodInfoFastRef.QueryableDefaultIfEmptyMethod.MakeGenericMethod(rightElementType), right);

			var method = MethodInfoFastRef.QueryableJoinMethod.MakeGenericMethod(leftElementType, rightElementType, targetPath.Last().GetMemberReturnType(), projector.ReturnType);
			
			return Expression.Call(null, method, left, right, Expression.Quote(leftSelector), Expression.Quote(rightSelector), Expression.Quote(projector));
		}

		private static Type CreateFinalTupleType(Type previousType, IEnumerable<Type> types)
		{
			return types.Aggregate(previousType, (current, type) => typeof(LeftRightJoinInfo<,>).MakeGenericType(current, type));
		}

		internal static Expression CreateExpressionForPath(int currentIndex, PropertyPath targetPath, ParameterExpression parameterExpression, Dictionary<PropertyPath, int> indexByPath)
		{
			var targetIndex = indexByPath[targetPath];
			var delta = currentIndex - targetIndex;
			
			Expression retval = parameterExpression;

			for (var i = 0; i < delta; i++)
			{
				retval = Expression.Property(retval, "Left");
			}

			if (retval.Type.IsGenericType && retval.Type.GetGenericTypeDefinition() == typeof(LeftRightJoinInfo<,>))
			{
				retval = Expression.Property(retval, "Right");
			}

			return retval;	
		}

		protected Expression RewriteBasicProjection(MethodCallExpression methodCallExpression, bool forSelector)
		{
			Expression[] originalSelectors;
			var originalSource = methodCallExpression.Arguments[0];
			var source = this.Visit(originalSource);
			var sourceType = source.Type.GetGenericArguments()[0];
			var originalPredicateOrSelector = methodCallExpression.Arguments[1];

			if (methodCallExpression.Arguments.Count == 2)
			{
				originalSelectors = new[] { originalPredicateOrSelector };
			}
			else
			{
				originalSelectors = new[] { originalPredicateOrSelector, methodCallExpression.Arguments[2] };
			}

			var sourceParameterExpression = (originalPredicateOrSelector.StripQuotes()).Parameters[0];
			var result = ReferencedRelatedObjectPropertyGatherer.Gather(this.model, originalSelectors, sourceParameterExpression, forSelector);
			var memberAccessExpressionsNeedingJoins = result.ReferencedRelatedObjectByPath;
			var currentRootExpressionsByPath = result.RootExpressionsByPath;

			var predicateOrSelectors = result.ReducedExpressions;
			var predicateOrSelectorLambdas = predicateOrSelectors.Select(c => c.StripQuotes()).ToArray();

			if (memberAccessExpressionsNeedingJoins.Count > 0)
			{
				var replacementExpressionForPropertyPath = new Dictionary<PropertyPath, Expression>(PropertyPathEqualityComparer.Default);

				var referencedObjectPaths = memberAccessExpressionsNeedingJoins
					.OrderBy(c => c.Key.Length)
					.Select(c => c.Value)
					.ToList();

				var types = referencedObjectPaths
					.Select(c => c.FullAccessPropertyPath.Last.PropertyType)
					.ToList();
				
				var finalTupleType = CreateFinalTupleType(sourceType, types);
				var replacementExpressionsByPropertyPathForSelector = new Dictionary<PropertyPath, Expression>(PropertyPathEqualityComparer.Default);
				var parameter = Expression.Parameter(finalTupleType);

				var i = 1;
				var indexByPath = new Dictionary<PropertyPath, int>(PropertyPathEqualityComparer.Default);

				foreach (var value in referencedObjectPaths)
				{
					indexByPath[value.FullAccessPropertyPath] = i++;
				}

				indexByPath[PropertyPath.Empty] = 0;

				foreach (var x in currentRootExpressionsByPath)
				{
					indexByPath[x.Key] = 0;
				}

				foreach (var path in referencedObjectPaths.Select(c => c.FullAccessPropertyPath))
				{
					var replacement = CreateExpressionForPath(referencedObjectPaths.Count, path, parameter, indexByPath);

					replacementExpressionsByPropertyPathForSelector[path] = replacement;
				}

				replacementExpressionsByPropertyPathForSelector[PropertyPath.Empty] = CreateExpressionForPath(referencedObjectPaths.Count, PropertyPath.Empty, parameter, indexByPath);

				foreach (var value in replacementExpressionsByPropertyPathForSelector)
				{
					replacementExpressionForPropertyPath[value.Key] = value.Value;
				}

				var propertyPathsByOriginalExpression = referencedObjectPaths
					.SelectMany(d => d.TargetExpressions.Select(e => new { PropertyPath = d.FullAccessPropertyPath, Expression = e }))
					.ToDictionary(c => c.Expression, c => c.PropertyPath);

				foreach (var lambda in predicateOrSelectorLambdas)
				{
					propertyPathsByOriginalExpression[lambda.Parameters[0]] = PropertyPath.Empty;
				}

				var replacementExpressions = propertyPathsByOriginalExpression
					.ToDictionary(c => c.Key, c => replacementExpressionsByPropertyPathForSelector[c.Value]);

				var index = 1;
				var currentLeft = source;

				foreach (var referencedObjectPath in referencedObjectPaths)
				{
					var property = referencedObjectPath.FullAccessPropertyPath[referencedObjectPath.FullAccessPropertyPath.Length - 1];
					var right = Expression.Constant(this.model.GetDataAccessObjects(property.PropertyType), typeof(DataAccessObjects<>).MakeGenericType(property.PropertyType));

					var join = MakeJoinCallExpression(index, currentLeft, right, referencedObjectPath.FullAccessPropertyPath, indexByPath, currentRootExpressionsByPath, sourceParameterExpression);

					currentLeft = join;
					index++;
				}

				Func<Expression, bool, Expression> replace = null;
				
				replace = (e, b) => ExpressionReplacer.Replace(e, c =>
				{
					Expression value;

					if (forSelector && b)
					{
						if (result.IncludedPropertyInfoByExpression.ContainsKey(c))
						{
							var x = replace(c, false);
							var y = result.IncludedPropertyInfoByExpression[c];

							var newList = y.Select(includedPropertyInfo => new IncludedPropertyInfo
							{
								RootExpression = x,
								FullAccessPropertyPath = includedPropertyInfo.FullAccessPropertyPath,
								IncludedPropertyPath = includedPropertyInfo.IncludedPropertyPath
								
							}).ToList();

							this.includedPropertyInfos[x] = newList;

							return x;
						}
					}

					if (replacementExpressions.TryGetValue(c, out value))
					{
						return value;
					}

					return null;
				});

				var newPredicatorOrSelectorBodies = predicateOrSelectorLambdas.Select(c => replace(c.Body, true)).ToArray();
				var newPredicateOrSelectors = newPredicatorOrSelectorBodies.Select(c => Expression.Lambda(c, parameter)).ToArray();

				MethodInfo newMethod;
				MethodCallExpression newCall;
				var newParameterType = newPredicateOrSelectors[0].Parameters[0].Type;

				if (methodCallExpression.Method.Name.StartsWith("Select")
				    || methodCallExpression.Method.Name.StartsWith("Where")
					|| methodCallExpression.Method.Name.EqualsIgnoreCase("OrderBy"))
				{
					if (methodCallExpression.Method.Name.StartsWith("Select"))
					{
						var projectionResultType = newPredicateOrSelectors[0].ReturnType;

						newMethod = methodCallExpression.Method.GetGenericMethodDefinition().MakeGenericMethod(newParameterType, projectionResultType);

						newCall = Expression.Call(null, newMethod, new[]
						{
							currentLeft,
							newPredicateOrSelectors[0]
						});
					}
					else if (methodCallExpression.Method.Name.StartsWith("Where"))
					{
						newMethod = methodCallExpression.Method.GetGenericMethodDefinition().MakeGenericMethod(newParameterType);

						newCall = Expression.Call(null, newMethod, new[]
						{
							currentLeft,
							newPredicateOrSelectors[0]
						});
					}
					else if (methodCallExpression.Method.Name.StartsWith("OrderBy"))
					{
						var keyType = newPredicateOrSelectors[0].ReturnType;

						newMethod = methodCallExpression.Method.GetGenericMethodDefinition().MakeGenericMethod(newParameterType, keyType);

						newCall = Expression.Call(null, newMethod, new[]
						{
							currentLeft,
							newPredicateOrSelectors[0]
						});
					}
					else
					{
						throw new InvalidOperationException();
					}

					if (newCall.Method.ReturnType.GetGenericArguments()[0].IsGenericType
						&& newCall.Method.ReturnType.GetGenericArguments()[0].GetGenericTypeDefinition() == typeof(LeftRightJoinInfo<,>))
					{
						var selectParameter = Expression.Parameter(newCall.Method.ReturnType.GetGenericArguments()[0]);
						var selectBody = CreateExpressionForPath(referencedObjectPaths.Count, PropertyPath.Empty, selectParameter, indexByPath);
						var selectCall = Expression.Lambda(selectBody, selectParameter);

						var selectMethod = MethodInfoFastRef.QueryableSelectMethod.MakeGenericMethod
						(
							selectParameter.Type,
							selectCall.ReturnType
						);

						newCall = Expression.Call(null, selectMethod, new Expression[] { newCall, selectCall });
					}

					this.replacementExpressionForPropertyPathsByJoin.Add(new Pair<Expression, Dictionary<PropertyPath, Expression>>(newCall, replacementExpressionForPropertyPath));
				}
				else if (methodCallExpression.Method.Name == ("GroupBy"))
				{
					var keyType = newPredicateOrSelectors[0].ReturnType;
					var elementType = methodCallExpression.Method.ReturnType.GetGenericArguments()[0].GetGenericArguments()[1];
					
					newMethod = methodCallExpression.Method
						.DeclaringType
						.GetMethods().Single(c => c.IsGenericMethod
						    && c.GetGenericArguments().Length == 3
						    && c.GetParameters().Length == 3
						    && c.GetParameters()[1].ParameterType.IsGenericType
						    && c.GetParameters()[2].ParameterType.IsGenericType
						    && c.GetParameters()[1].ParameterType.GetGenericTypeDefinition() == typeof(Expression<>)
						    && c.GetParameters()[2].ParameterType.GetGenericTypeDefinition() == typeof(Expression<>)
						    && c.GetParameters()[1].ParameterType.GetGenericArguments()[0].IsGenericType
						    && c.GetParameters()[2].ParameterType.GetGenericArguments()[0].IsGenericType
						    && c.GetParameters()[1].ParameterType.GetGenericArguments()[0].GetGenericTypeDefinition() == typeof(Func<,>)
						    && c.GetParameters()[2].ParameterType.GetGenericArguments()[0].GetGenericTypeDefinition() == typeof(Func<,>))
						.MakeGenericMethod(newParameterType, keyType, elementType);

					var elementSelectorParameter = Expression.Parameter(newParameterType);
					var elementSelectorBody = CreateExpressionForPath(referencedObjectPaths.Count, PropertyPath.Empty, elementSelectorParameter, indexByPath);
					var elementSelector = Expression.Lambda(elementSelectorBody, elementSelectorParameter);

					newCall = Expression.Call(null, newMethod, new []
					{
						currentLeft,
						newPredicateOrSelectors[0],
						elementSelector
					});
				}
				else
				{
					throw new InvalidOperationException("Method: " + methodCallExpression.Method);
				}

				this.replacementExpressionForPropertyPathsByJoin.Add(new Pair<Expression, Dictionary<PropertyPath, Expression>>(newCall, replacementExpressionForPropertyPath));

				return newCall;
			}
			else
			{
				if (source == originalSource
					&& predicateOrSelectors.SequenceEqual(originalSelectors, ObjectReferenceIdentityEqualityComparer<Expression>.Default))
				{
					return methodCallExpression;
				}
				else
				{
					return Expression.Call
					(
						methodCallExpression.Object,
						methodCallExpression.Method,
						predicateOrSelectors.Prepend(source).ToArray()
					);
				}
			}
		}
	}
}
