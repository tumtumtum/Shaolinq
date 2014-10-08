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
	public class ReferencedRelatedObject
	{
		public PropertyInfo[] PropertyPath { get; private set; }
		public HashSet<Expression> TargetExpressions { get; private set; }

		public ReferencedRelatedObject(PropertyInfo[] propertyPath)
		{
			this.PropertyPath = propertyPath;
			this.TargetExpressions = new HashSet<Expression>(ObjectReferenceIdentityEqualityComparer<Expression>.Default);
		}

		public ReferencedRelatedObject(IEnumerable<PropertyInfo> propertyPath)
		{
			this.PropertyPath = propertyPath.ToArray();
			this.TargetExpressions = new HashSet<Expression>(ObjectReferenceIdentityEqualityComparer<Expression>.Default);
		}

		public override int GetHashCode()
		{
			return this.PropertyPath.Aggregate(this.PropertyPath.Length, (current, value) => current ^ value.GetHashCode());
		}

		public override bool Equals(object obj)
		{
			if (obj == this)
			{
				return true;
			}

			var value = obj as ReferencedRelatedObject;

			return value != null && ArrayEqualityComparer<PropertyInfo>.Default.Equals(this.PropertyPath, value.PropertyPath);
		}
	}

	public struct ReferencedRelatedObjectPropertyGathererResults
	{
		public Expression ReducedExpression { get; set; }
		public Dictionary<PropertyInfo[], Expression> RootExpressionsByPath { get; set; }
		public Dictionary<PropertyInfo[], ReferencedRelatedObject> ReferencedRelatedObjectByPath { get; set; }
	}

	public  class ReferencedRelatedObjectPropertyGatherer
		: SqlExpressionVisitor
	{
		private bool disableCompare;
		private readonly bool forProjection; 
		private readonly DataAccessModel model;
		private readonly ParameterExpression sourceParameterExpression;
		private readonly Dictionary<PropertyInfo[], Expression> rootExpressionsByPath = new Dictionary<PropertyInfo[], Expression>(ArrayEqualityComparer<PropertyInfo>.Default);
		private readonly Dictionary<PropertyInfo[], ReferencedRelatedObject> results = new Dictionary<PropertyInfo[], ReferencedRelatedObject>(ArrayEqualityComparer<PropertyInfo>.Default);
		
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

		public static ReferencedRelatedObjectPropertyGathererResults Gather(DataAccessModel model, Expression expression, ParameterExpression sourceParameterExpression, bool forProjection)
		{
			var gatherer = new ReferencedRelatedObjectPropertyGatherer(model, sourceParameterExpression, forProjection);
			
			var reducedExpression = gatherer.Visit(expression);

			return new ReferencedRelatedObjectPropertyGathererResults
			{
				ReducedExpression = reducedExpression,
				ReferencedRelatedObjectByPath = gatherer.results,
				RootExpressionsByPath = gatherer.rootExpressionsByPath
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
				var selector = (LambdaExpression)methodCallExpression.Arguments[1];
				var newSelector = ExpressionReplacer.Replace(selector.Body, selector.Parameters[0], methodCallExpression.Arguments[0]);

				this.Visit(newSelector);

				var retval = this.Visit(methodCallExpression.Arguments[0]);

				return retval;
			}

			return base.VisitMethodCall(methodCallExpression);
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
			var root = memberExpression.Expression;
			var x = false;

			if (memberExpression.Type.IsDataAccessObjectType())
			{
				if (forProjection)
				{
					x = true;

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

				root = currentExpression.Expression;
				currentExpression = root as MemberExpression;
			}

			if (root != sourceParameterExpression)
			{
				return base.VisitMemberAccess(memberExpression);
			}

			visited.Reverse();

			var i = 0;
			currentExpression = expression;

			while (currentExpression != null && currentExpression.Member is PropertyInfo)
			{
				var path = visited.Take(visited.Count - i).ToArray();

				ReferencedRelatedObject objectInfo;

				if (path.Length == 0)
				{
					break;
				}

				if (!path[path.Length - 1].ReflectedType.IsDataAccessObjectType())
				{
					rootExpressionsByPath[path] = memberExpression;

					break;
				}

				if (!results.TryGetValue(path, out objectInfo))
				{
					objectInfo = new ReferencedRelatedObject(path);
					results[path] = objectInfo;
				}

				if (currentExpression == expression)
				{
					if (memberExpression.Expression is MemberExpression)
					{
						if (x)
						{
							objectInfo.TargetExpressions.Add(memberExpression);
						}
						else
						{
							objectInfo.TargetExpressions.Add(memberExpression.Expression);
						}
					}
				}

				i++;
				currentExpression = currentExpression.Expression as MemberExpression;
			}

			return memberExpression;
		}
	}
}
