// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Shaolinq.TypeBuilding;
using PropertyPath = Shaolinq.Persistence.Linq.ObjectPath<System.Reflection.PropertyInfo>;

namespace Shaolinq.Persistence.Linq.Optimizers
{
	public  class ReferencedRelatedObjectPropertyGatherer
		: SqlExpressionVisitor
	{
		private int nesting = 0;
		private bool disableCompare;
		private Expression currentParent;
		private readonly bool forProjection; 
		private readonly DataAccessModel model;
		private ParameterExpression sourceParameterExpression;
		private List<ReferencedRelatedObject> referencedRelatedObjects = new List<ReferencedRelatedObject>();
		private readonly HashSet<IncludedPropertyInfo> includedPropertyInfos = new HashSet<IncludedPropertyInfo>(IncludedPropertyInfoEqualityComparer.Default);
		private readonly Dictionary<PropertyPath, Expression> rootExpressionsByPath = new Dictionary<PropertyPath, Expression>(PropertyPathEqualityComparer.Default);
		private readonly Dictionary<PropertyPath, ReferencedRelatedObject> results = new Dictionary<PropertyPath, ReferencedRelatedObject>(PropertyPathEqualityComparer.Default);
		
		private class DisableCompareContext
			: IDisposable
		{
			private readonly bool savedDisableCompare;
			private readonly ReferencedRelatedObjectPropertyGatherer gatherer;

			public DisableCompareContext(ReferencedRelatedObjectPropertyGatherer gatherer)
			{
				this.gatherer = gatherer;
				this.savedDisableCompare = gatherer.disableCompare;
				gatherer.disableCompare = true;
			}

			public void Dispose()
			{
				this.gatherer.disableCompare = this.savedDisableCompare;
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

		public static ReferencedRelatedObjectPropertyGathererResults Gather(DataAccessModel model, IList<Tuple<ParameterExpression, Expression>> expressions, bool forProjection)
		{
			var gatherer = new ReferencedRelatedObjectPropertyGatherer(model, null, forProjection);

            var reducedExpressions = expressions.Select(c =>
            {
				gatherer.sourceParameterExpression = c.Item1;
				return gatherer.Visit(c.Item2);
            }).ToArray();

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

		protected override Expression VisitMethodCall(MethodCallExpression methodCallExpression)
		{
			if (methodCallExpression.Method.IsGenericMethod
				&& methodCallExpression.Method.GetGenericMethodDefinition() == MethodInfoFastRef.DataAccessObjectExtensionsIncludeMethod)
			{
				if (!this.forProjection)
				{
					throw new InvalidOperationException();
				}

				var selector = methodCallExpression.Arguments[1].StripQuotes();
				var newSelector = SqlExpressionReplacer.Replace(selector.Body, selector.Parameters[0], methodCallExpression.Arguments[0]);

				var originalReferencedRelatedObjects = this.referencedRelatedObjects;
				var originalParent = this.currentParent;

				this.currentParent = methodCallExpression.Arguments[0];

				this.referencedRelatedObjects = new List<ReferencedRelatedObject>();

				this.nesting++;

				this.Visit(newSelector);

				if (this.referencedRelatedObjects.Count == 0)
				{
					return this.Visit(methodCallExpression.Arguments[0]);
				}

				var referencedRelatedObject = this.referencedRelatedObjects[0];
				
				this.referencedRelatedObjects = originalReferencedRelatedObjects;
				this.currentParent = originalParent;

				var retval = this.Visit(methodCallExpression.Arguments[0]);

				if (this.nesting > 1 && (retval != this.sourceParameterExpression) && retval is MemberExpression)
				{
					// For supporting: Select(c => c.Include(d => d.Address.Include(e => e.Region)))

					var prefixProperties = new List<PropertyInfo>();
					var current = (MemberExpression)retval;

					while (current != null)
					{
						if (!current.Member.ReflectedType.IsDataAccessObjectType()
							|| current == this.currentParent)
						{
							break;
						}

						prefixProperties.Add((PropertyInfo)current.Member);
						
						if (current.Expression == this.sourceParameterExpression)
						{
							break;
						}
						
						current = current.Expression as MemberExpression;
					}

					prefixProperties.Reverse();

					this.AddIncludedProperty(this.sourceParameterExpression, referencedRelatedObject, new PropertyPath(c => c.Name, prefixProperties));
				}
				else
				{
					this.AddIncludedProperty(retval, referencedRelatedObject, PropertyPath.Empty);
				}

				this.nesting--;

				return retval;
			}

			return base.VisitMethodCall(methodCallExpression);
		}

		private void AddIncludedProperty(Expression root, ReferencedRelatedObject referencedRelatedObject, PropertyPath prefixPath)
		{
			for (var i = 0; i < referencedRelatedObject.IncludedPropertyPath.Length + prefixPath.Length; i++)
			{
				var fullAccessPropertyPath = new PropertyPath(c => c.Name, referencedRelatedObject.FullAccessPropertyPath.Take(referencedRelatedObject.FullAccessPropertyPath.Length - i));
				var currentPropertyPath = new PropertyPath(c => c.Name, prefixPath.Concat(referencedRelatedObject.IncludedPropertyPath.Take(referencedRelatedObject.IncludedPropertyPath.Length - i)));

				var includedPropertyInfo = new IncludedPropertyInfo
				{
					RootExpression = root,
					FullAccessPropertyPath = fullAccessPropertyPath,
					IncludedPropertyPath = currentPropertyPath
				};

				this.includedPropertyInfos.Add(includedPropertyInfo);
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
				if (this.forProjection)
				{
					memberIsDataAccessObjectGatheringForProjection = true;

					expression = memberExpression;
				}
				else
				{
					expression = memberExpression.Expression as MemberExpression;

					if (expression == null || !expression.Expression.Type.IsDataAccessObjectType())
					{
						return memberExpression;
					}
				}
			}
			else
			{
				if (memberExpression.Expression == null)
				{
					return memberExpression;
				}

				var typeDescriptor = this.model.TypeDescriptorProvider.GetTypeDescriptor(memberExpression.Expression.Type);

				if (typeDescriptor == null)
				{
					return memberExpression;
				}

				var property = typeDescriptor.GetPropertyDescriptorByPropertyName(memberExpression.Member.Name);

				if (property.IsPrimaryKey && memberExpression.Expression is MemberExpression)
				{
					expression = ((MemberExpression) memberExpression.Expression).Expression as MemberExpression;
				}
				else
				{
					expression = memberExpression.Expression as MemberExpression;
				}
			}

			var currentExpression = expression;

			while (currentExpression != null && currentExpression.Member is PropertyInfo)
			{
				visited.Add(currentExpression);

				root = currentExpression.Expression;
				currentExpression = root as MemberExpression;
			}

			var includedPathSkip = 0;

			var i = 0;

			foreach (var current in visited)
			{
				if (!current.Member.ReflectedType.IsDataAccessObjectType()
					|| current == this.currentParent /* @see: Test_Select_Project_Related_Object_And_Include1 */)
				{
					root = current;
					includedPathSkip = visited.Count - i;
					
					break;
				}

				i++;
			}

			visited.Reverse();

			i = 0;
			currentExpression = expression;

			while (currentExpression != null && currentExpression.Member is PropertyInfo)
			{
				var path = new PropertyPath(c => c.Name, visited.Select(c=> (PropertyInfo)c.Member).Take(visited.Count - i).ToArray());
				
				ReferencedRelatedObject objectInfo;

				if (path.Length == 0)
				{
					break;
				}

				if (!path.Last.ReflectedType.IsDataAccessObjectType())
				{
					this.rootExpressionsByPath[path] = currentExpression;
					
					break;
				}

				if (!this.results.TryGetValue(path, out objectInfo))
				{
					var x = i + includedPathSkip - 1;
					var includedPropertyPath = new PropertyPath(c => c.Name, path.Skip(includedPathSkip));
					var objectExpression = x >= 0 ? visited[x] : root;

					objectInfo = new ReferencedRelatedObject(path, includedPropertyPath, objectExpression, sourceParameterExpression);

					this.results[path] = objectInfo;
				}

				this.referencedRelatedObjects.Add(objectInfo);

				if (memberIsDataAccessObjectGatheringForProjection)
				{
					objectInfo.TargetExpressions.Add(currentExpression);
				}
				else if (currentExpression == expression)
				{
					objectInfo.TargetExpressions.Add(expression);
				}

				i++;
				currentExpression = currentExpression.Expression as MemberExpression;
			}

			return memberExpression;
		}
	}
}
