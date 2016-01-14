// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Platform;
using Platform.Collections;
using Platform.Reflection;
using Shaolinq.Persistence.Linq.Optimizers;
using Shaolinq.TypeBuilding;
using PropertyPath = Shaolinq.Persistence.Linq.ObjectPath<System.Reflection.PropertyInfo>;

namespace Shaolinq.Persistence.Linq
{
	public class RelatedPropertiesJoinExpander
		: SqlExpressionVisitor
	{
		protected struct RewriteBasicProjectionResults
		{
			public bool Changed { get; set; }
			public Expression NewSource { get; set; }
			public List<LambdaExpression> NewSelectors { get; set; }
			public List<ReferencedRelatedObject> ReferencedObjectPaths { get; set; }
			public Dictionary<PropertyPath, Expression> ReplacementExpressionsByPropertyPath { get; set; }
			public Dictionary<PropertyPath, int> IndexByPath { get; set; }

			public RewriteBasicProjectionResults(bool changed)
				: this()
			{
				this.Changed = changed;
			}
		}

		private readonly DataAccessModel model;
		private readonly Dictionary<Expression, List<IncludedPropertyInfo>> includedPropertyInfos = new Dictionary<Expression, List<IncludedPropertyInfo>>();
		private readonly List<Tuple<Expression, Dictionary<PropertyPath, Expression>>> replacementExpressionForPropertyPathsByJoin = new List<Tuple<Expression, Dictionary<PropertyPath, Expression>>>();

		private RelatedPropertiesJoinExpander(DataAccessModel model)
		{
			this.model = model;
		}

		public static RelatedPropertiesJoinExpanderResults Expand(DataAccessModel model, Expression expression)
		{
			expression = SqlProjectionSelectExpander.Expand(expression);

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
			case "OrderBy":
			case "GroupBy":
			case "Where":
			case "WhereForUpdate":
			case "Min":
			case "Max":
			case "Average":
			case "Sum":
			case "Count":
			case "First":
			case "FirstOrDefault":
			case "Single":
			case "SingleOrDefault":
			case "SelectMany":
			case "Include":
				return this.RewriteBasicProjection(methodCallExpression, false);
			case "Select":
			case "SelectForUpdate":
				return this.RewriteBasicProjection(methodCallExpression, true);
			case "Join":
			{
				var outer = methodCallExpression.Arguments[0];
				var inner = methodCallExpression.Arguments[1];
				var outerKeySelector = methodCallExpression.Arguments[2].StripQuotes();
				var innerKeySelector = methodCallExpression.Arguments[3].StripQuotes();
				var resultSelector = methodCallExpression.Arguments[4].StripQuotes();

				var outerResult = this.RewriteBasicProjection
				(
					outer,
					new[]
					{
						new Tuple<LambdaExpression, ParameterExpression>(outerKeySelector, outerKeySelector.Parameters[0]),
						new Tuple<LambdaExpression, ParameterExpression>(resultSelector, resultSelector.Parameters[0])
					},
					false
				);

				if (outerResult.Changed)
				{
					outer = outerResult.NewSource;
					outerKeySelector = outerResult.NewSelectors[0];
					resultSelector = Expression.Lambda(outerResult.NewSelectors[1].Body, outerResult.NewSelectors[1].Parameters[0], resultSelector.Parameters[1]);
				}

				var innerResult = this.RewriteBasicProjection
				(
					inner,
					new[]
					{
						new Tuple<LambdaExpression, ParameterExpression>(innerKeySelector, innerKeySelector.Parameters[0]),
						new Tuple<LambdaExpression, ParameterExpression>(resultSelector, resultSelector.Parameters[1])
					},
					false
				);

				if (innerResult.Changed)
				{
					inner = innerResult.NewSource;
					innerKeySelector = innerResult.NewSelectors[0];
					resultSelector = Expression.Lambda(innerResult.NewSelectors[1].Body, resultSelector.Parameters[0], innerResult.NewSelectors[1].Parameters[1]);
				}

				if (outerResult.Changed || innerResult.Changed)
				{
					var newMethodInfo = MethodInfoFastRef.QueryableJoinMethod.MakeGenericMethod
					(
						outer.Type.GetSequenceElementType(),
						inner.Type.GetSequenceElementType(),
						outerKeySelector.Type.GetGenericArguments()[1],
						resultSelector.Type.GetGenericArguments()[2]
					);

					return Expression.Call(newMethodInfo, outer, inner, outerKeySelector, innerKeySelector, resultSelector);
				}

				return base.VisitMethodCall(methodCallExpression);
			}
			default:
				if (methodCallExpression.Arguments[0].Type.IsQueryable()
					&& (methodCallExpression.Arguments.Count == 1
					|| (methodCallExpression.Arguments.Count == 2 && methodCallExpression.Arguments[1].Type.IsExpressionTree())))
				{
					return this.RewriteBasicProjection(methodCallExpression, false);
				}

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

		private MethodCallExpression MakeJoinCallExpression(int index, Expression left, Expression right, PropertyPath targetPath, Dictionary<PropertyPath, int> indexByPath, Dictionary<PropertyPath, Expression> rootExpressionsByPath, Expression sourceParameterExpression)
		{
			Expression leftObject;

			var leftElementType = left.Type.GetGenericArguments()[0];
			var rightElementType = right.Type.GetGenericArguments()[0];

			var rootPath = targetPath.PathWithoutLast();
			var leftSelectorParameter = Expression.Parameter(leftElementType);

			if (index == 1 && rootExpressionsByPath.ContainsKey(rootPath))
			{
				leftObject = rootExpressionsByPath[rootPath];

				leftObject = SqlExpressionReplacer.Replace(leftObject, c => c == sourceParameterExpression ? leftSelectorParameter : null);
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

			LambdaExpression leftSelector, rightSelector;

			if (targetPath.Last.GetMemberReturnType().GetGenericTypeDefinitionOrNull() == typeof(RelatedDataAccessObjects<>))
			{
				leftSelector = Expression.Lambda(leftObject, leftSelectorParameter);

				var typeDescriptor = this.model.TypeDescriptorProvider.GetTypeDescriptor(leftObject.Type);
				var relationship = typeDescriptor
					.GetRelationshipInfos()
					.Where(c => c.RelationshipType == RelationshipType.ParentOfOneToMany)
					.Single(c => c.ReferencingProperty.PropertyName == targetPath.Last.Name);

				var rightSelectorParameter = Expression.Parameter(rightElementType);
				rightSelector = Expression.Lambda(Expression.Property(rightSelectorParameter, relationship.TargetProperty), rightSelectorParameter);
			}
			else
			{
				leftSelector = Expression.Lambda(Expression.Property(leftObject, targetPath.Last().Name), leftSelectorParameter);

				var rightSelectorParameter = Expression.Parameter(rightElementType);
				rightSelector = Expression.Lambda(rightSelectorParameter, rightSelectorParameter);
			}
			
			var projector = MakeJoinProjector(leftElementType, rightElementType);
			var method = JoinHelperExtensions.LeftJoinMethod.MakeGenericMethod(leftElementType, rightElementType, leftSelector.ReturnType, projector.ReturnType);
			
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

		private Expression Reselect(MethodCallExpression methodCall, List<ReferencedRelatedObject> referencedObjectPaths, Dictionary<PropertyPath, int> indexByPath)
		{
			if (methodCall.Method.ReturnType.GetSequenceElementType()?.GetGenericTypeDefinitionOrNull() == typeof(LeftRightJoinInfo<,>))
			{
				var selectParameter = Expression.Parameter(methodCall.Method.ReturnType.GetGenericArguments()[0]);
				var selectBody = CreateExpressionForPath(referencedObjectPaths.Count, PropertyPath.Empty, selectParameter, indexByPath);
				var selectCall = Expression.Lambda(selectBody, selectParameter);

				var selectMethod = MethodInfoFastRef.QueryableSelectMethod.MakeGenericMethod
				(
					selectParameter.Type,
					selectCall.ReturnType
				);

				return Expression.Call(null, selectMethod, new Expression[] { methodCall, selectCall });
			}

			return methodCall;
		}
		
		protected Expression RewriteBasicProjection(MethodCallExpression methodCallExpression, bool forSelector)
		{
			MethodCallExpression newCall = null;

			var selectors = methodCallExpression
				.Arguments
				.Where(c => c.Type.GetGenericTypeDefinitionOrNull() == typeof(Expression<>))
				.Select(c => this.Visit(c).StripQuotes())
				.ToArray();

			var result = this.RewriteBasicProjection(methodCallExpression.Arguments[0], selectors.Select(c => new Tuple<LambdaExpression, ParameterExpression>(c, c.Parameters[0])).ToArray(), forSelector);

			if (!result.Changed)
			{
				if (result.NewSource == methodCallExpression.Arguments[0] && result.NewSelectors.SequenceEqual(selectors, ObjectReferenceIdentityEqualityComparer<Expression>.Default))
				{
					return base.VisitMethodCall(methodCallExpression);
				}
			}

			Type newParameterType;
			MethodInfo methodWithElementSelector;
				
			switch (methodCallExpression.Method.Name)
			{
			case "Select":
			case "SelectForUpdate":
			{
				var projectionResultType = result.NewSelectors[0].ReturnType;
				newParameterType = result.NewSelectors[0].Parameters[0].Type;
				methodWithElementSelector = methodCallExpression.Method.GetGenericMethodDefinition().MakeGenericMethod(newParameterType, projectionResultType);

				newCall = Expression.Call(null, methodWithElementSelector, new[]
				{
					result.NewSource,
					result.NewSelectors[0]
				});

				break;
			}
			case "SelectMany":
			{
				var projectionResultType = result.NewSelectors[1].ReturnType;
				newParameterType = result.NewSelectors[0].Parameters[0].Type;
				var collectionType = result.NewSelectors[0].ReturnType.GetSequenceElementType();
				methodWithElementSelector = methodCallExpression.Method.GetGenericMethodDefinition().MakeGenericMethod(newParameterType, collectionType, projectionResultType);

				newCall = Expression.Call(null, methodWithElementSelector, new[]
				{
					result.NewSource,
					result.NewSelectors[0],
					result.NewSelectors[1]
				});

				break;
			}
			case "Where":
			case "WhereForUpdate":
				newParameterType = result.NewSelectors[0].Parameters[0].Type;
				methodWithElementSelector = methodCallExpression.Method.GetGenericMethodDefinition().MakeGenericMethod(newParameterType);

				newCall = Expression.Call(null, methodWithElementSelector, new[]
				{
					result.NewSource,
					result.NewSelectors[0]
				});

				break;
			case "OrderBy":
			case "Min":
			case "Max":
			case "Sum":
			case "Average":
			case "Count":
			case "First":
			case "FirstOrDefault":
			case "Single":
			case "SingleOrDefault":
				var resultType = result.NewSelectors.Count > 0 ? result.NewSelectors[0].ReturnType : null;
				newParameterType = result.NewSource.Type.GetSequenceElementType();
				var method = methodCallExpression.Method.GetGenericMethodDefinition();
				methodWithElementSelector = method.GetGenericArguments().Length == 1
					? method.MakeGenericMethod(newParameterType)
					: method.MakeGenericMethod(newParameterType, resultType);

				newCall = Expression.Call
				(
					null,
					methodWithElementSelector, 
					result.NewSelectors.Count > 0 ? new[] { result.NewSource, result.NewSelectors[0] } : new[] { result.NewSource }
				);

				break;
			case "GroupBy":
				var keyType = result.NewSelectors[0].ReturnType;
				newParameterType = result.NewSelectors[0].Parameters[0].Type;
				var elementType = methodCallExpression.Method.ReturnType.GetGenericArguments()[0].GetGenericArguments()[1];

				methodWithElementSelector = MethodInfoFastRef
					.QueryableGroupByWithElementSelectorMethod
					.MakeGenericMethod(newParameterType, keyType, elementType);
		
				if (methodCallExpression.Method.GetGenericMethodOrRegular() == MethodInfoFastRef.QueryableGroupByMethod)
				{
					if (result.Changed)
					{
						var elementSelectorParameter = Expression.Parameter(newParameterType);
						var elementSelectorBody = CreateExpressionForPath(result.ReferencedObjectPaths.Count, PropertyPath.Empty, elementSelectorParameter, result.IndexByPath);
						var elementSelector = Expression.Lambda(elementSelectorBody, elementSelectorParameter);

						newCall = Expression.Call(null, methodWithElementSelector, result.NewSource, result.NewSelectors[0], elementSelector);
					}
					else
					{
						newCall = Expression.Call(null, methodCallExpression.Method, result.NewSource, result.NewSelectors[0]);
					}
				}
				else if (methodCallExpression.Method.GetGenericMethodOrRegular() == MethodInfoFastRef.QueryableGroupByWithElementSelectorMethod)
				{
					if (result.Changed)
					{
						var existingElementSelector = methodCallExpression.Arguments[2].StripQuotes();
						var elementSelectorParameter = Expression.Parameter(newParameterType);
						var pathExpression = CreateExpressionForPath(result.ReferencedObjectPaths.Count, PropertyPath.Empty, elementSelectorParameter, result.IndexByPath);
						var newBody = SqlExpressionReplacer.Replace(existingElementSelector.Body, existingElementSelector.Parameters[0], pathExpression);

						var elementSelector = Expression.Lambda(newBody, elementSelectorParameter);

						newCall = Expression.Call(null, methodWithElementSelector, result.NewSource, result.NewSelectors[0], elementSelector);
					}
					else
					{
						newCall = Expression.Call(null, methodCallExpression.Method, result.NewSource, result.NewSelectors[0], result.NewSelectors[1]);
					}
				}
				else
				{
					throw new NotSupportedException($"Unsupport method when using explicit joins: {methodCallExpression.Method}");
				}

				break;
			default:
				
				resultType = result.NewSelectors.Count > 0 ? result.NewSelectors[0].ReturnType : null;
				newParameterType = result.NewSource.Type.GetSequenceElementType();
				method = methodCallExpression.Method.GetGenericMethodDefinition();
				methodWithElementSelector = method.GetGenericArguments().Length == 1
					? method.MakeGenericMethod(newParameterType)
					: method.MakeGenericMethod(newParameterType, resultType);

				newCall = Expression.Call
				(
					null,
					methodWithElementSelector,
					result.NewSelectors.Count > 0 ? new[] { result.NewSource, result.NewSelectors[0] } : new[] { result.NewSource }
				);

				break;
			}

			this.replacementExpressionForPropertyPathsByJoin.Add(new Tuple<Expression, Dictionary<PropertyPath, Expression>>(newCall, result.ReplacementExpressionsByPropertyPath));

			return this.Reselect(newCall, result.ReferencedObjectPaths, result.IndexByPath);
		}

		protected RewriteBasicProjectionResults RewriteBasicProjection(Expression originalSource, Tuple<LambdaExpression, ParameterExpression>[] originalSelectors, bool forProjection)
		{
			var source = this.Visit(originalSource);
			var sourceType = source.Type.GetGenericArguments()[0];

			var result = ReferencedRelatedObjectPropertyGatherer.Gather(this.model, originalSelectors.Select(c => new Tuple<ParameterExpression, Expression>(c.Item2, c.Item1)).ToList(), forProjection);
			var memberAccessExpressionsNeedingJoins = result.ReferencedRelatedObjectByPath;
			var currentRootExpressionsByPath = result.RootExpressionsByPath;

			var predicateOrSelectors = result.ReducedExpressions.Select(c => c.StripQuotes()).ToArray();
			var predicateOrSelectorLambdas = predicateOrSelectors.Select(c => c.StripQuotes()).ToArray();

			if (memberAccessExpressionsNeedingJoins.Count == 0)
			{
				return new RewriteBasicProjectionResults
				{
					NewSource = source,
					NewSelectors = predicateOrSelectors.ToList(),
					ReferencedObjectPaths = new List<ReferencedRelatedObject>()
				};
			}

			var replacementExpressionForPropertyPath = new Dictionary<PropertyPath, Expression>(PropertyPathEqualityComparer.Default);

			var referencedObjectPaths = memberAccessExpressionsNeedingJoins
				.OrderBy(c => c.Key.Length)
				.Select(c => c.Value)
				.ToList();

			var types = referencedObjectPaths
				.Select(c => c.FullAccessPropertyPath.Last.PropertyType.JoinedObjectType())
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
				var right = Expression.Constant(this.model.GetDataAccessObjects(property.PropertyType.JoinedObjectType()), typeof(DataAccessObjects<>).MakeGenericType(property.PropertyType.JoinedObjectType()));

				var join = MakeJoinCallExpression(index, currentLeft, right, referencedObjectPath.FullAccessPropertyPath, indexByPath, currentRootExpressionsByPath, referencedObjectPath.SourceParameterExpression);

				currentLeft = join;
				index++;
			}

			Func<Expression, bool, Expression> replace = null;

			replace = (e, b) => SqlExpressionReplacer.Replace(e, c =>
			{
				if (forProjection && b)
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

				return replacementExpressions.GetValueOrDefault(c);
			});

			var newPredicatorOrSelectorBodies = predicateOrSelectorLambdas
				.Select(c => replace(c.Body, true))
				.ToList();

			var newPredicateOrSelectors = newPredicatorOrSelectorBodies
				.Zip(originalSelectors, (x, y) => new { body = x, parameters = y.Item1.Parameters, sourceParameter = y.Item2 })
				.Select(c => Expression.Lambda(c.body, c.parameters.Select(d => d == c.sourceParameter ? parameter : c.sourceParameter)))
				.ToList();
				
			return new RewriteBasicProjectionResults(true)
			{
				NewSource = currentLeft,
				IndexByPath = indexByPath,
				NewSelectors = newPredicateOrSelectors,
				ReferencedObjectPaths = referencedObjectPaths,
				ReplacementExpressionsByPropertyPath = replacementExpressionForPropertyPath
			};
		}
	}
}
