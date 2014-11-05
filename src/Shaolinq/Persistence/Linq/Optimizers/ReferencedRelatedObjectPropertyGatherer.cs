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
		private readonly Dictionary<PropertyInfo[], Expression> rootExpressionsByPath = new Dictionary<PropertyInfo[], Expression>(ArrayEqualityComparer<PropertyInfo>.Default);
		private readonly Dictionary<PropertyInfo[], ReferencedRelatedObject> results = new Dictionary<PropertyInfo[], ReferencedRelatedObject>(ArrayEqualityComparer<PropertyInfo>.Default);
		private PropertyInfo[] lastPropertyPathSuffix;
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

			return new ReferencedRelatedObjectPropertyGathererResults
			{
				ReducedExpressions = expressions.Select(gatherer.Visit).ToArray(),
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

				var propertyPath = this.referencedRelatedObjects[0].PropertyPath;
				var suffix = this.lastPropertyPathSuffix.ToArray();

				this.referencedRelatedObjects = originalReferencedRelatedObjects;
				this.currentParent = originalParent;

				var retval = this.Visit(methodCallExpression.Arguments[0]);

				if (nesting > 1 &&  (retval != sourceParameterExpression) && retval is MemberExpression)
				{
					// Support includes like:
					// Select(c => c.Include(d => d.Address.Include(e => e.Region)));

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

					AddIncludedProperty(sourceParameterExpression, propertyPath, prefixProperties.Take(nesting - 1).Concat(suffix).ToArray());
				}
				else
				{
					AddIncludedProperty(retval, propertyPath, suffix);
				}

				nesting--;

				return retval;
			}

			return base.VisitMethodCall(methodCallExpression);
		}

		private void AddIncludedProperty(Expression root, PropertyInfo[] propertyPath, PropertyInfo[] suffix)
		{
			/*
			for (var i = propertyPath.Length - 1; i >= 1; i--)
			{
				var rootPath = propertyPath.Take(i).ToArray();

				if (rootExpressionsByPath.TryGetValue(rootPath, out root))
				{
					propertyPath = propertyPath.Skip(i).ToArray();

					break;
				}
			}

			if (propertyPath.Length == 0)
			{
				return;
			}*/

			for (var i = 1; i <= suffix.Length; i++)
			{
				var delta = suffix.Length - i;

				if (propertyPath.Length - delta <= 0)
				{
					continue;
				}

				var includedPropertyInfo = new IncludedPropertyInfo
				{
					RootExpression = root,
					PropertyPath = propertyPath.Take(propertyPath.Length - delta).ToArray(),
					SuffixPropertyPath = suffix.Take(i).ToArray()
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
			var visited = new List<PropertyInfo>();
			var visitedExpressions = new List<Expression>();
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

			var currentExpression = expression;

			while (currentExpression != null && currentExpression.Member is PropertyInfo)
			{
				visited.Add((PropertyInfo)currentExpression.Member);
				visitedExpressions.Add(currentExpression);

				root = currentExpression.Expression;
				currentExpression = root as MemberExpression;
			}

			visited.Reverse();
			visitedExpressions.Reverse();

			var i = 0;
			currentExpression = expression;

			var foundParent = false;

			var suffix = new List<PropertyInfo>();

			while (currentExpression != null && currentExpression.Member is PropertyInfo)
			{
				var path = visited.Take(visited.Count - i).ToArray();
				
				ReferencedRelatedObject objectInfo;

				if (path.Length == 0)
				{
					break;
				}

				if (currentExpression == this.currentParent)
				{
					foundParent = true;
				}

				if (!foundParent)
				{
					suffix.Insert(0, (PropertyInfo)currentExpression.Member);
				}

				if (!path.Last().ReflectedType.IsDataAccessObjectType())
				{
					rootExpressionsByPath[path] = currentExpression;

					break;
				}

				if (!results.TryGetValue(path, out objectInfo))
				{
					objectInfo = new ReferencedRelatedObject(path, suffix.ToArray());
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

			this.lastPropertyPathSuffix = suffix.ToArray();

			return memberExpression;
		}
	}
}
