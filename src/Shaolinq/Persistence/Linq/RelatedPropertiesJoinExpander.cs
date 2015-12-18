// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Platform;
using Platform.Reflection;
using Shaolinq.Persistence.Linq.Optimizers;
using Shaolinq.TypeBuilding;
using PropertyPath = Shaolinq.Persistence.Linq.ObjectPath<System.Reflection.PropertyInfo>;

namespace Shaolinq.Persistence.Linq
{
	internal static class JoinHelperExtensions
	{
		public static readonly MethodInfo LeftJoinMethod = TypeUtils.GetMethod(() => ((IQueryable<string>)null).LeftJoin((IEnumerable<string>)null, x => "", y => "", (x, y) => "")).GetGenericMethodDefinition();

		private static Expression GetSourceExpression<TSource>(IEnumerable<TSource> source)
		{
			var queryable = source as IQueryable<TSource>;

			return queryable?.Expression ?? Expression.Constant(source, typeof(IEnumerable<TSource>));
		}

		public static IQueryable<TResult> LeftJoin<TOuter, TInner, TKey, TResult>(this IQueryable<TOuter> outer, IEnumerable<TInner> inner, Expression<Func<TOuter, TKey>> outerKeySelector, Expression<Func<TInner, TKey>> innerKeySelector, Expression<Func<TOuter, TInner, TResult>> resultSelector)
		{
			return outer.Provider.CreateQuery<TResult>(Expression.Call(null, ((MethodInfo)MethodBase.GetCurrentMethod()).MakeGenericMethod(typeof(TOuter), typeof(TInner), typeof(TKey), typeof(TResult)), outer.Expression, GetSourceExpression(inner), Expression.Quote(outerKeySelector), Expression.Quote(innerKeySelector), Expression.Quote(resultSelector)));
		}
	}

	public class RelatedPropertiesJoinExpanderResults
	{
		public Expression ProcessedExpression { get; set; }
		public Dictionary<Expression, List<IncludedPropertyInfo>> IncludedPropertyInfos { get; set; }
		private readonly List<Tuple<Expression, Dictionary<PropertyPath, Expression>>> replacementExpressionForPropertyPathsByJoin;

		internal RelatedPropertiesJoinExpanderResults(List<Tuple<Expression, Dictionary<PropertyPath, Expression>>> replacementExpressionForPropertyPathsByJoin)
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

				if (currentJoin == this.replacementExpressionForPropertyPathsByJoin[index].Item1)
				{
					indexFound = index;
				}

				if (index > indexFound)
				{
					continue;
				}
				
				if (this.replacementExpressionForPropertyPathsByJoin[index].Item2.TryGetValue(propertyPath, out retval))
				{
					return retval;	
				}
			}

			for (index = this.replacementExpressionForPropertyPathsByJoin.Count - 1; index >= 0; index--)
			{
				Expression retval;

				if (currentJoin == this.replacementExpressionForPropertyPathsByJoin[index].Item1)
				{
					indexFound = index;
				}

				if (index > indexFound)
				{
					continue;
				}

				if (this.replacementExpressionForPropertyPathsByJoin[index].Item2.TryGetValue(propertyPath, out retval))
				{
					return retval;
				}

				if (this.replacementExpressionForPropertyPathsByJoin[index].Item2.TryGetValue(PropertyPath.Empty, out retval))
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
				return this.RewriteBasicProjection(methodCallExpression, false);
			case "Select":
			case "SelectForUpdate":
				return this.RewriteBasicProjection(methodCallExpression, true);
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

			var leftSelector = Expression.Lambda(Expression.Property(leftObject, targetPath.Last().Name), leftSelectorParameter);

			var rightSelectorParameter = Expression.Parameter(rightElementType);
			var rightSelector = Expression.Lambda(rightSelectorParameter, rightSelectorParameter);

			var projector = MakeJoinProjector(leftElementType, rightElementType);

			var method = JoinHelperExtensions.LeftJoinMethod.MakeGenericMethod(leftElementType, rightElementType, targetPath.Last().GetMemberReturnType(), projector.ReturnType);
			
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
			if (methodCall.Method.ReturnType.GetGenericArguments()[0].GetGenericTypeDefinitionOrNull() == typeof(LeftRightJoinInfo<,>))
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

			var selectors = methodCallExpression.Arguments.Where(c => c.Type.GetGenericTypeDefinitionOrNull() == typeof(Expression<>)).Select(c => c.StripQuotes()).ToArray();
			var result = this.RewriteBasicProjection(methodCallExpression.Arguments[0], selectors, forSelector);

			if (result.Changed)
			{
				Type keyType;
				MethodInfo methodWithElementSelector;
				Type newParameterType;
				
				switch (methodCallExpression.Method.Name)
				{
				case "Select":
				case "SelectForUpdate":
					var projectionResultType = result.NewSelectors[0].ReturnType;
					newParameterType = result.NewSelectors[0].Parameters[0].Type;
					methodWithElementSelector = methodCallExpression.Method.GetGenericMethodDefinition().MakeGenericMethod(newParameterType, projectionResultType);

					newCall = Expression.Call(null, methodWithElementSelector, new[]
					{
						result.NewSource,
						result.NewSelectors[0]
					});

					break;
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
					keyType = result.NewSelectors[0].ReturnType;
					newParameterType = result.NewSelectors[0].Parameters[0].Type;
					methodWithElementSelector = methodCallExpression.Method.GetGenericMethodDefinition().MakeGenericMethod(newParameterType, keyType);

					newCall = Expression.Call(null, methodWithElementSelector, new[]
					{
						result.NewSource,
						result.NewSelectors[0]
					});

					break;
				case "GroupBy":

					keyType = result.NewSelectors[0].ReturnType;
					newParameterType = result.NewSelectors[0].Parameters[0].Type;
					var elementType = methodCallExpression.Method.ReturnType.GetGenericArguments()[0].GetGenericArguments()[1];

					methodWithElementSelector = MethodInfoFastRef
						.QueryableGroupByWithElementSelectorMethod
						.MakeGenericMethod(newParameterType, keyType, elementType);

					if (methodCallExpression.Method.GetGenericMethodOrRegular() == MethodInfoFastRef.QueryableGroupByMethod)
					{
						var elementSelectorParameter = Expression.Parameter(newParameterType);
						var elementSelectorBody = CreateExpressionForPath(result.ReferencedObjectPaths.Count, PropertyPath.Empty, elementSelectorParameter, result.IndexByPath);
						var elementSelector = Expression.Lambda(elementSelectorBody, elementSelectorParameter);

						newCall = Expression.Call(null, methodWithElementSelector, result.NewSource, result.NewSelectors[0], elementSelector);
					}
					else if (methodCallExpression.Method.GetGenericMethodOrRegular() == MethodInfoFastRef.QueryableGroupByWithElementSelectorMethod)
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
						throw new NotSupportedException($"Unsupport method when using explicit joins: {methodCallExpression.Method}");
					}

					break;
				}
			}

			if (newCall != null)
			{
				this.replacementExpressionForPropertyPathsByJoin.Add(new Tuple<Expression, Dictionary<PropertyPath, Expression>>(newCall, result.ReplacementExpressionsByPropertyPath));

				return Reselect(newCall, result.ReferencedObjectPaths, result.IndexByPath);
			}

			if (result.NewSource == methodCallExpression.Arguments[0] && result.NewSelectors.SequenceEqual(selectors, ObjectReferenceIdentityEqualityComparer<Expression>.Default))
			{
				return base.VisitMethodCall(methodCallExpression);
			}
			else
			{
				return Expression.Call
				(
					methodCallExpression.Object,
					methodCallExpression.Method,
					result.NewSelectors.Prepend(result.NewSource).ToArray()
				);
			}
		}


		protected struct RewriteBasicProjectionResults
		{
			public bool Changed { get; set; }
			public Expression NewSource { get; set; }
			public LambdaExpression[] NewSelectors { get; set; }
			public List<ReferencedRelatedObject> ReferencedObjectPaths { get; set; }
			public Dictionary<PropertyPath, Expression> ReplacementExpressionsByPropertyPath { get; set; }
			public Dictionary<PropertyPath, int> IndexByPath { get; set; }

			public RewriteBasicProjectionResults (bool changed)
				: this()
			{
				this.Changed = changed;
			}
		}

		protected RewriteBasicProjectionResults RewriteBasicProjection(Expression originalSource, LambdaExpression[] originalSelectors, bool forProjection)
		{
			var source = this.Visit(originalSource);
			var sourceType = source.Type.GetGenericArguments()[0];

			var result = ReferencedRelatedObjectPropertyGatherer.Gather(this.model, originalSelectors.Select(c => new Tuple<ParameterExpression, Expression>(c.StripQuotes().Parameters[0], c)).ToList(), forProjection);
			var memberAccessExpressionsNeedingJoins = result.ReferencedRelatedObjectByPath;
			var currentRootExpressionsByPath = result.RootExpressionsByPath;

			var predicateOrSelectors = result.ReducedExpressions.Select(c => c.StripQuotes()).ToArray();
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

					var join = MakeJoinCallExpression(index, currentLeft, right, referencedObjectPath.FullAccessPropertyPath, indexByPath, currentRootExpressionsByPath, referencedObjectPath.SourceParameterExpression);

					currentLeft = join;
					index++;
				}

				Func<Expression, bool, Expression> replace = null;

				replace = (e, b) => SqlExpressionReplacer.Replace(e, c =>
				{
					Expression value;

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

					if (replacementExpressions.TryGetValue(c, out value))
					{
						return value;
					}

					return null;
				});

				var newPredicatorOrSelectorBodies = predicateOrSelectorLambdas.Select(c => replace(c.Body, true)).ToArray();
				var newPredicateOrSelectors = newPredicatorOrSelectorBodies.Select(c => Expression.Lambda(c, parameter)).ToArray();
				
				return new RewriteBasicProjectionResults(true)
				{
					NewSource = currentLeft,
					IndexByPath = indexByPath,
					NewSelectors = newPredicateOrSelectors,
					ReferencedObjectPaths = referencedObjectPaths,
					ReplacementExpressionsByPropertyPath = replacementExpressionForPropertyPath
				};
			}
			
			return new RewriteBasicProjectionResults
			{
				NewSource = source,
				NewSelectors = predicateOrSelectors.ToArray()
			};
		}
	}
}
