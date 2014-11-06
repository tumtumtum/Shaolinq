// Copyright (c) 2007-2014 Thong Nguyen (tumtumtum@gmail.com)

using System;
using Platform;
using System.Linq;
using System.Reflection;
using System.Linq.Expressions;
using System.Collections.Generic;
using Shaolinq.Persistence.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Optimizers
{
	public  class ReferencedRelatedObjectPropertyGatherer
		: SqlExpressionVisitor
	{
		private bool disableCompare;
		private readonly bool forProjection; 
		private readonly DataAccessModel model;
		private readonly ParameterExpression sourceParameterExpression;
		private List<ReferencedRelatedObject> referencedRelatedObjects = new List<ReferencedRelatedObject>();
		private readonly HashSet<IncludedPropertyInfo> includedPropertyInfos = new HashSet<IncludedPropertyInfo>(IncludedPropertyInfoEqualityComparer.Default);
		private readonly HashSet<Expression> rootExpressions = new HashSet<Expression>();
		private readonly Dictionary<PropertyPath, Expression> rootExpressionsByPath = new Dictionary<PropertyPath, Expression>(PropertyPathEqualityComparer.Default);
		private readonly Dictionary<PropertyPath, ReferencedRelatedObject> results = new Dictionary<PropertyPath, ReferencedRelatedObject>(PropertyPathEqualityComparer.Default);
		private readonly Dictionary<ParameterExpression, Expression> expressionsByParameter = new Dictionary<ParameterExpression, Expression>();
		private Expression currentParent;

		private class DisableCompareContext
			: IDisposable
		{
			private readonly bool savedDisableCompare;
			private readonly ReferencedRelatedObjectPropertyGatherer gatherer;

			public DisableCompareContext(ReferencedRelatedObjectPropertyGatherer gatherer)
			{
				this.gatherer = gatherer;
				savedDisableCompare = gatherer.disableCompare;
				gatherer.disableCompare = true;
			}

			public void Dispose()
			{
				this.gatherer.disableCompare = savedDisableCompare;
			}
		}

		protected IDisposable AcquireDisableCompareContext()
		{
			return new DisableCompareContext(this);
		}
		
		public ReferencedRelatedObjectPropertyGatherer(DataAccessModel model, ParameterExpression sourceParameterExpression, bool forProjection)
		{
			this.model = model;
			this.sourceParameterExpression = sourceParameterExpression;
			this.forProjection = forProjection;
		}

		public static ReferencedRelatedObjectPropertyGathererResults Gather(DataAccessModel model, Expression[] expressions, ParameterExpression sourceParameterExpression, bool forProjection)
		{
			var gatherer = new ReferencedRelatedObjectPropertyGatherer(model, sourceParameterExpression, forProjection);

			var reducedExpressions = expressions.Select(gatherer.Visit).ToArray();

			return new ReferencedRelatedObjectPropertyGathererResults
			{
				ReducedExpressions = reducedExpressions,
				ReferencedRelatedObjectByPath = gatherer.results,
				RootExpressionsByPath = gatherer.rootExpressionsByPath,
				IncludedPropertyInfoByExpression = gatherer
					.includedPropertyInfos
					.GroupBy(c => c.RootExpression)
					.ToDictionary(c => c.Key, c => c.ToList())
			};
		}

		protected override Expression VisitBinary(BinaryExpression binaryExpression)
		{
			using (this.AcquireDisableCompareContext())
			{
				return base.VisitBinary(binaryExpression);
			}
		}

		private int nesting = 0;
		
		protected override Expression VisitMethodCall(MethodCallExpression methodCallExpression)
		{
			if (methodCallExpression.Method.IsGenericMethod
				&& methodCallExpression.Method.GetGenericMethodDefinition() == MethodInfoFastRef.DataAccessObjectExtensionsIncludeMethod)
			{
				if (!this.forProjection)
				{
					throw new InvalidOperationException();
				}

				var selector = (LambdaExpression)QueryBinder.StripQuotes(methodCallExpression.Arguments[1]);
				var newSelector = ExpressionReplacer.Replace(selector.Body, selector.Parameters[0], methodCallExpression.Arguments[0]);

				var originalReferencedRelatedObjects = referencedRelatedObjects;
				var originalParent = this.currentParent;

				this.currentParent = methodCallExpression.Arguments[0];

				referencedRelatedObjects = new List<ReferencedRelatedObject>();

				nesting++;

				this.Visit(newSelector);

				if (referencedRelatedObjects.Count == 0)
				{
					return this.Visit(methodCallExpression.Arguments[0]);
				}

				var referencedRelatedObject = this.referencedRelatedObjects[0];
				
				this.referencedRelatedObjects = originalReferencedRelatedObjects;
				this.currentParent = originalParent;

				var retval = this.Visit(methodCallExpression.Arguments[0]);

				if (nesting > 1 &&  (retval != sourceParameterExpression) && retval is MemberExpression)
				{
					// For supporting: Select(c => c.Include(d => d.Address.Include(e => e.Region)))

					var prefixProperties = new List<PropertyInfo>();
					var current = (MemberExpression)retval;

					while (current != null)
					{
						prefixProperties.Add((PropertyInfo)current.Member);

						if (current.Expression == sourceParameterExpression)
						{
							break;
						}

						current = current.Expression as MemberExpression;
					}

					prefixProperties.Reverse();

					AddIncludedProperty(sourceParameterExpression, referencedRelatedObject.RootExpression, (MemberExpression[])referencedRelatedObject.ExpressionPath);
				}
				else
				{
					AddIncludedProperty(retval, referencedRelatedObject.RootExpression, (MemberExpression[])referencedRelatedObject.ExpressionPath);
				}

				nesting--;

				return retval;
			}

			return base.VisitMethodCall(methodCallExpression);
		}

		private void AddIncludedProperty(Expression root, Expression memberAccessRoot, MemberExpression[] expressionPath)
		{
			for (var i = 0; i < expressionPath.Length; i++)
			{
				var currentPropertyPath = new PropertyPath(expressionPath.Take(expressionPath.Length - i).Select(c => (PropertyInfo)c.Member));
				var currentExpression = expressionPath[expressionPath.Length - i - 1];

				if (currentExpression == memberAccessRoot)
				{
					break;
				}
				
				var includedPropertyInfo = new IncludedPropertyInfo
				{
					RootExpression = root,
					PropertyPath = currentPropertyPath
				};

				includedPropertyInfos.Add(includedPropertyInfo);
			}
		}

		protected override Expression VisitConditional(ConditionalExpression expression)
		{
			Expression test;

			using (this.AcquireDisableCompareContext())
			{
				test = this.Visit(expression.Test);
			}

			var ifTrue = this.Visit(expression.IfTrue);
			var ifFalse = this.Visit(expression.IfFalse);

			if (test != expression.Test || ifTrue != expression.IfTrue || ifFalse != expression.IfFalse)
			{
				return Expression.Condition(test, ifTrue, ifFalse);
			}
			else
			{
				return expression;
			}
		}

		protected override Expression VisitMemberAccess(MemberExpression memberExpression)
		{
			MemberExpression expression; 
			var visited = new List<MemberExpression>();
			var root = memberExpression.Expression;
			var memberIsDataAccessObjectGatheringForProjection = false;

			if (memberExpression.Type.IsDataAccessObjectType())
			{
				if (forProjection)
				{
					memberIsDataAccessObjectGatheringForProjection = true;

					expression = memberExpression;
				}
				else
				{
					return memberExpression;
				}
			}
			else
			{
				var typeDescriptor = this.model.TypeDescriptorProvider.GetTypeDescriptor(memberExpression.Expression.Type);

				if (typeDescriptor == null)
				{
					return memberExpression;
				}

				var property = typeDescriptor.GetPropertyDescriptorByPropertyName(memberExpression.Member.Name);

				if (property.IsPrimaryKey)
				{
					return memberExpression;
				}

				expression = memberExpression.Expression as MemberExpression;
			}

			var rootTake = 0;
			Expression rootExpression = null;
			var currentExpression = expression;

			while (currentExpression != null && currentExpression.Member is PropertyInfo)
			{
				visited.Add(currentExpression);

				root = currentExpression.Expression;
				currentExpression = root as MemberExpression;
			}

			visited.Reverse();
			
			var i = 0;
			currentExpression = expression;

			while (currentExpression != null && currentExpression.Member is PropertyInfo)
			{
				var path = new PropertyPath(visited.Select(c=> (PropertyInfo)c.Member).Take(visited.Count - i).ToArray());
				var expressionPath = visited.Take(visited.Count - i).ToArray();

				ReferencedRelatedObject objectInfo;

				if (path.Length == 0)
				{
					break;
				}

				if (!path.Last.ReflectedType.IsDataAccessObjectType())
				{
					rootExpressionsByPath[path] = currentExpression;

					break;
				}

				if (!results.TryGetValue(path, out objectInfo))
				{
					objectInfo = new ReferencedRelatedObject(root, path, expressionPath);
					results[path] = objectInfo;
				}

				referencedRelatedObjects.Add(objectInfo);

				if (memberIsDataAccessObjectGatheringForProjection)
				{
					objectInfo.TargetExpressions.Add(currentExpression);
				}
				else if (currentExpression == expression && memberExpression.Expression is MemberExpression)
				{
					objectInfo.TargetExpressions.Add(memberExpression.Expression);
				}

				i++;
				currentExpression = currentExpression.Expression as MemberExpression;
			}

			return memberExpression;
		}
	}
}
