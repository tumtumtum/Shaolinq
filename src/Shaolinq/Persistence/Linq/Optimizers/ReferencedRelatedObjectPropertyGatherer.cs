// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Platform;
using Platform.Reflection;
using Shaolinq.TypeBuilding;
using PropertyPath = Shaolinq.Persistence.Linq.ObjectPath<System.Reflection.PropertyInfo>;

namespace Shaolinq.Persistence.Linq.Optimizers
{
	public  class ReferencedRelatedObjectPropertyGatherer
		: SqlExpressionVisitor
	{
		private int nesting;
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
				return SqlExpressionReplacer.Replace(gatherer.Visit(c.Item2), d => d.StripForIncludeScanning());
			}).ToArray();

			return new ReferencedRelatedObjectPropertyGathererResults
			{
				ReducedExpressions = reducedExpressions,
				ReferencedRelatedObjects = gatherer.results.Values.ToList(),
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

		private readonly Dictionary<ParameterExpression, Expression> expressionByParameter = new Dictionary<ParameterExpression, Expression>();
		
		private Expression GetExpression(Expression expression)
		{
			var parameterExpression = expression as ParameterExpression;

			if (parameterExpression != null && this.expressionByParameter.TryGetValue(parameterExpression, out var retval))
			{
				return retval;
			}

			return expression;
		}

		protected override Expression VisitMethodCall(MethodCallExpression methodCallExpression)
		{
			if (methodCallExpression.Method.GetGenericMethodOrRegular() == MethodInfoFastRef.QueryableSelectMethod)
			{
				this.expressionByParameter[methodCallExpression.Arguments[1].StripQuotes().Parameters[0]] = methodCallExpression.Arguments[0];

				return base.VisitMethodCall(methodCallExpression);
			}

			if (methodCallExpression.Method.GetGenericMethodOrRegular() == MethodInfoFastRef.DataAccessObjectExtensionsIncludeMethod)
			{
				if (!this.forProjection)
				{
					throw new InvalidOperationException();
				}

				var selector = methodCallExpression.Arguments[1].StripQuotes();
				var newSelector = SqlExpressionReplacer.Replace(selector.Body, selector.Parameters[0], methodCallExpression.Arguments[0]);

				var originalReferencedRelatedObjects = this.referencedRelatedObjects;
				var originalParent = this.currentParent;

				this.currentParent = methodCallExpression.Arguments[0].StripForIncludeScanning();

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

				var retval = this.Visit(methodCallExpression.Arguments[0].StripForIncludeScanning());

				if (this.nesting > 1 && (retval != this.sourceParameterExpression) && retval is MemberExpression)
				{
					// For supporting: Select(c => c.Include(d => d.Address.Include(e => e.Region)))

					var prefixProperties = new List<PropertyInfo>();
					var current = (MemberExpression)retval.StripForIncludeScanning();

					while (current != null)
					{
						if (!current.Member.ReflectedType.IsTypeRequiringJoin()
							|| current == this.currentParent)
						{
							break;
						}
						
						prefixProperties.Add((PropertyInfo)current.Member);
						
						if (current.Expression.StripForIncludeScanning() == this.sourceParameterExpression)
						{
							break;
						}
						
						current = current.Expression.StripForIncludeScanning() as MemberExpression;
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
			var fullAccessPropertyPath = new PropertyPath(c => c.Name, referencedRelatedObject.FullAccessPropertyPath);
			var currentPropertyPath = new PropertyPath(c => c.Name, prefixPath.Concat(referencedRelatedObject.IncludedPropertyPath));
			
			while (currentPropertyPath.Length > 0)
			{
				var includedPropertyInfo = new IncludedPropertyInfo
				{
					RootExpression = root,
					FullAccessPropertyPath = fullAccessPropertyPath,
					IncludedPropertyPath = currentPropertyPath
				};

				this.includedPropertyInfos.Add(includedPropertyInfo);

				fullAccessPropertyPath = fullAccessPropertyPath.RemoveLast();
				currentPropertyPath = currentPropertyPath.RemoveLast();
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
			var root = memberExpression.Expression.StripForIncludeScanning();
			var memberIsDataAccessObjectGatheringForProjection = false;

			// Don't perform implicit joins for RelatedDataAccessObject collections as those currently turn into N+1 queries
			if (this.nesting < 1 && (memberExpression.Type.GetSequenceElementType()?.IsDataAccessObjectType() ?? false))
			{
				return base.VisitMemberAccess(memberExpression);
			}
			
			if (memberExpression.Type.IsTypeRequiringJoin())
			{
				if (this.forProjection)
				{
					memberIsDataAccessObjectGatheringForProjection = true;

					expression = memberExpression.StripForIncludeScanning() as MemberExpression;
				}
				else
				{
					expression = memberExpression.Expression.StripForIncludeScanning() as MemberExpression;

					if (expression == null || !expression.Expression.StripForIncludeScanning().Type.IsTypeRequiringJoin())
					{
						return memberExpression;
					}
				}
			}
			else
			{
				if (memberExpression.Expression?.StripForIncludeScanning() == null)
				{
					return memberExpression;
				}

				var typeDescriptor = this.model.TypeDescriptorProvider.GetTypeDescriptor(memberExpression.Expression.StripForIncludeScanning().Type);

				if (typeDescriptor == null)
				{
					return Expression.MakeMemberAccess(this.Visit(memberExpression.Expression.StripForIncludeScanning()), memberExpression.Member);
				}

				var property = typeDescriptor.GetPropertyDescriptorByPropertyName(memberExpression.Member.Name);

				if (property.IsPrimaryKey && memberExpression.Expression.StripForIncludeScanning() is MemberExpression)
				{
					expression = ((MemberExpression)memberExpression.Expression.StripForIncludeScanning()).Expression.StripForIncludeScanning() as MemberExpression;
				}
				else
				{
					expression = memberExpression.Expression.StripForIncludeScanning() as MemberExpression;
				}
			}

			var currentExpression = expression.StripForIncludeScanning() as MemberExpression;

			while (currentExpression?.Member is PropertyInfo)
			{
				visited.Add(currentExpression);

				root = currentExpression.Expression.StripForIncludeScanning();

				currentExpression = root as MemberExpression;

				if (currentExpression == null)
				{
					root = this.GetExpression(root).StripForIncludeScanning();
					currentExpression = root as MemberExpression;
				}
			}

			var includedPathSkip = 0;

			var i = 0;

			foreach (var current in visited)
			{
				if (!current.Member.ReflectedType.IsTypeRequiringJoin()
					|| current == this.currentParent /* @see: Test_Select_Project_Related_Object_And_Include1 */)
				{
					includedPathSkip = visited.Count - i;
					
					break;
				}

				i++;
			}

			visited.Reverse();

			i = 0;
			currentExpression = expression;

			while (currentExpression?.Member is PropertyInfo)
			{
				var path = new PropertyPath(c => c.Name, visited.Select(c=> (PropertyInfo)c.Member).Take(visited.Count - i).ToArray());


				if (path.Length == 0)
				{
					break;
				}

				if (!path.Last.ReflectedType.IsTypeRequiringJoin())
				{
					this.rootExpressionsByPath[path] = currentExpression;
					
					break;
				}

				if (!this.results.TryGetValue(path, out var objectInfo))
				{
					var includedPropertyPath = new PropertyPath(c => c.Name, path.Skip(includedPathSkip));

					objectInfo = new ReferencedRelatedObject(path, includedPropertyPath, this.sourceParameterExpression);

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
				currentExpression = currentExpression.Expression.StripForIncludeScanning() as MemberExpression;
			}

			return memberExpression;
		}
	}
}
