// Copyright (c) 2007-2014 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Platform;
using Platform.Reflection;
using Shaolinq.Persistence.Linq.Optimizers;
using Shaolinq.Persistence.Linq.Expressions;

namespace Shaolinq.Persistence.Linq
{
	internal struct LeftRightJoinInfo<L, R>
	{
		public L Left { get; set; }
		public R Right { get; set; }
	}

	internal class PropertyPath
	{
		public PropertyInfo[] Path { get; set; }
		public Expression ExpressionRoot { get; set; }
		
	}

	internal class PropertyPathEqualityComparer
		: IEqualityComparer<PropertyPath>
	{
		public static readonly PropertyPathEqualityComparer Default = new PropertyPathEqualityComparer();

		public bool Equals(PropertyPath x, PropertyPath y)
		{
			return x.ExpressionRoot == y.ExpressionRoot && ArrayEqualityComparer<PropertyInfo>.Default.Equals(x.Path, y.Path);
		}

		public int GetHashCode(PropertyPath obj)
		{
			var retval = obj.ExpressionRoot.GetHashCode();

			return obj.Path.Aggregate(retval, (current, path) => current ^ path.Name.GetHashCode());
		}
	}

	public class RelatedPropertiesJoinExpanderResults
	{	
		public Expression ProcessedExpression { get; set; }
		public Dictionary<Expression, List<IncludedPropertyInfo>> IncludedPropertyInfos { get; set; }
		public Dictionary<PropertyInfo[], Expression> ReplacementExpressionForPropertyPath { get; set; }
	}

	public class RelatedPropertiesJoinExpander
		: SqlExpressionVisitor
	{
		private readonly DataAccessModel model;
		private readonly Dictionary<PropertyInfo[], Expression> rootExpressionsByPath = new Dictionary<PropertyInfo[], Expression>();
		private readonly Dictionary<Expression, List<IncludedPropertyInfo>> includedPropertyInfos = new Dictionary<Expression, List<IncludedPropertyInfo>>();
		private readonly Dictionary<PropertyInfo[], Expression> replacementExpressionForPropertyPath = new Dictionary<PropertyInfo[], Expression>(ArrayEqualityComparer<PropertyInfo>.Default);

		private RelatedPropertiesJoinExpander(DataAccessModel model)
		{
			this.model = model;
		}

		public static RelatedPropertiesJoinExpanderResults Expand(DataAccessModel model, Expression expression)
		{
			var visitor = new RelatedPropertiesJoinExpander(model);

			var processedExpression = visitor.Visit(expression);

			return new RelatedPropertiesJoinExpanderResults
			{
				ProcessedExpression = processedExpression,
				IncludedPropertyInfos = visitor.includedPropertyInfos,
				ReplacementExpressionForPropertyPath = visitor.replacementExpressionForPropertyPath 
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

		private static MethodCallExpression MakeJoinCallExpression(int index, Expression left, Expression right, PropertyInfo[] targetPath, Dictionary<PropertyInfo[], int> indexByPath, Dictionary<PropertyInfo[], Expression> rootExpressionsByPath, Expression sourceParameterExpression)
		{
			Expression leftObject;

			var leftElementType = left.Type.GetGenericArguments()[0];
			var rightElementType = right.Type.GetGenericArguments()[0];

			var rootPath = targetPath.Take(targetPath.Length - 1).ToArray();
			var leftSelectorParameter = Expression.Parameter(leftElementType);

			if (!rootExpressionsByPath.TryGetValue(rootPath, out leftObject))
			{
				leftObject = CreateExpressionForPath(index - 1, rootPath, leftSelectorParameter, indexByPath);
			}
			else
			{
				leftObject = ExpressionReplacer.Replace(leftObject, c =>
				{
					if (c == sourceParameterExpression)
					{
						return leftSelectorParameter;
					}

					return null;
				});
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

		internal static Expression CreateExpressionForPath(int currentIndex, PropertyInfo[] targetPath, ParameterExpression parameterExpression, Dictionary<PropertyInfo[], int> indexByPath)
		{
			int delta;

			if (currentIndex == indexByPath.Count)
			{
				delta = currentIndex;
			}
			else if (targetPath.Length == 0)
			{
				return parameterExpression;
			}
			else
			{
				if (!indexByPath.ContainsKey(targetPath))
				{
					delta = currentIndex;
				}
				else
				{
					var targetIndex = indexByPath[targetPath];

					delta = currentIndex - targetIndex;
				}
			}

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
			var source = this.Visit(methodCallExpression.Arguments[0]);
			var sourceType = source.Type.GetGenericArguments()[0];
			var predicateOrSelector = (LambdaExpression)QueryBinder.StripQuotes(methodCallExpression.Arguments[1]);
			var sourceParameterExpression = predicateOrSelector.Parameters[0];
			var result = ReferencedRelatedObjectPropertyGatherer.Gather(this.model, predicateOrSelector, sourceParameterExpression, forSelector);
			var memberAccessExpressionsNeedingJoins = result.ReferencedRelatedObjectByPath;
			var currentRootExpressionsByPath = result.RootExpressionsByPath;
			
			predicateOrSelector = (LambdaExpression)result.ReducedExpression;

			if (memberAccessExpressionsNeedingJoins.Count > 0)
			{
				ReferencedRelatedObjectPropertyGatherer.Gather(this.model, predicateOrSelector, sourceParameterExpression, forSelector);

				var referencedObjectPaths = memberAccessExpressionsNeedingJoins
					.OrderBy(c => c.Key.Length)
					.Select(c => c.Value)
					.ToList();

				var types = referencedObjectPaths
					.Select(c => c.PropertyPath[c.PropertyPath.Length - 1].PropertyType)
					.ToList();
				
				var finalTupleType = CreateFinalTupleType(sourceType, types);
				var replacementExpressionsByPropertyPathForSelector = new Dictionary<PropertyInfo[], Expression>(ArrayEqualityComparer<PropertyInfo>.Default);
				var parameter = Expression.Parameter(finalTupleType);

				var i = 0;
				var indexByPath = new Dictionary<PropertyInfo[], int>(ArrayEqualityComparer<PropertyInfo>.Default);

				foreach (var value in referencedObjectPaths)
				{
					indexByPath[value.PropertyPath] = i++;
				}

				foreach (var path in referencedObjectPaths)
				{
					var replacement = CreateExpressionForPath(referencedObjectPaths.Count - 1, path.PropertyPath, parameter, indexByPath);

					replacementExpressionsByPropertyPathForSelector[path.PropertyPath] = replacement;
				}

				replacementExpressionsByPropertyPathForSelector[new PropertyInfo[0]] = CreateExpressionForPath(referencedObjectPaths.Count, new PropertyInfo[0], parameter, indexByPath);

				foreach (var value in replacementExpressionsByPropertyPathForSelector)
				{
					this.replacementExpressionForPropertyPath[value.Key] = value.Value;
				}

				var propertyPathsByOriginalExpression = referencedObjectPaths
					.SelectMany(d => d.TargetExpressions.Select(e => new { d.PropertyPath, Expression = e }))
					.ToDictionary(c => c.Expression, c => c.PropertyPath);

				propertyPathsByOriginalExpression[predicateOrSelector.Parameters[0]] = new PropertyInfo[0];

				var replacementExpressions = propertyPathsByOriginalExpression
					.ToDictionary(c => c.Key, c => replacementExpressionsByPropertyPathForSelector[c.Value]);

				var index = 0;
				var currentLeft = source;

				foreach (var referencedObjectPath in referencedObjectPaths)
				{
					var property = referencedObjectPath.PropertyPath[referencedObjectPath.PropertyPath.Length - 1];
					var right = Expression.Constant(this.model.GetDataAccessObjects(property.PropertyType), typeof(DataAccessObjects<>).MakeGenericType(property.PropertyType));

					var join = MakeJoinCallExpression(index, currentLeft, right, referencedObjectPath.PropertyPath, indexByPath, currentRootExpressionsByPath, sourceParameterExpression);

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

							var newList = new List<IncludedPropertyInfo>();

							foreach (var includedPropertyInfo in y)
							{
								newList.Add(new IncludedPropertyInfo
								{
									PropertyPath = includedPropertyInfo.PropertyPath,
									SuffixPropertyPath = includedPropertyInfo.SuffixPropertyPath,
									RootExpression = x
								});
							}

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

				/*foreach (var value in currentRootExpressionsByPath)
				{
					this.rootExpressionsByPath[value.Key] = replace(value.Value);
				}*/

				/*
				if (forSelector)
				{
					foreach (var keyValuePair in result.IncludedPropertyInfoByExpression)
					{
						keyValuePair.Value.RootExpression = replace(keyValuePair.Value.RootExpression);
						this.includedPropertyInfos[keyValuePair.Value.RootExpression] = keyValuePair.Value;
					}
				}*/

				var newPredicateOrSelectorBody = replace(predicateOrSelector.Body, true);
				var newPredicateOrSelector = Expression.Lambda(newPredicateOrSelectorBody, parameter);

				MethodInfo newMethod;
				var newParameterType = newPredicateOrSelector.Parameters[0].Type;

				if (methodCallExpression.Method.Name.StartsWith("Select"))
				{
					var projectionResultType = newPredicateOrSelector.ReturnType;

					newMethod = methodCallExpression.Method.GetGenericMethodDefinition().MakeGenericMethod(newParameterType, projectionResultType);
				}
				else
				{
					newMethod = methodCallExpression.Method.GetGenericMethodDefinition().MakeGenericMethod(newParameterType);
				}

				var newCall = Expression.Call(null, newMethod, new[]
				{
					currentLeft,
					newPredicateOrSelector
				});

				if (newCall.Method.ReturnType != methodCallExpression.Method.ReturnType)
				{
					var selectParameter = Expression.Parameter(newParameterType);
					var selectBody = CreateExpressionForPath(referencedObjectPaths.Count, new PropertyInfo[0], selectParameter, indexByPath);
					var selectCall = Expression.Lambda(selectBody, selectParameter);

					var selectMethod = MethodInfoFastRef.QueryableSelectMethod.MakeGenericMethod
					(
						selectParameter.Type,
						selectCall.ReturnType
					);

					newCall = Expression.Call(null, selectMethod, new Expression[] { newCall, selectCall });
				}

				return newCall;
			}
			else
			{
				if (source == methodCallExpression.Arguments[0])
				{
					return methodCallExpression;
				}
				else
				{
					return Expression.Call
					(
						methodCallExpression.Object,
						methodCallExpression.Method,
						new[] { source, methodCallExpression.Arguments[1]}
					);
				}
			}
		}
	}
}
