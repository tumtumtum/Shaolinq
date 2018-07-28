// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)

using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Platform.Reflection;
using Shaolinq.TypeBuilding;

namespace Shaolinq.Persistence.Linq.Optimizers
{
	/// <summary>
	/// Moves Include statements that include sub collections outside of Skip/Take if possible otherwise eliminate the include altogether.
	/// </summary>
	/// <remarks>
	/// <code>
	/// query.Include(c => c.Shops).Skip(1).Take(10)
	/// ->
	/// query.Skip(1).Take(10).Include(c => c.Shops)
	/// </code>
	/// Test: ComplexIncludeTests.Test3
	/// </remarks>
	public class ShiftSubCollectionIncludesOutsideSkipTakeAmender
		: SqlExpressionVisitor
	{
		public LambdaExpression selector;
		private bool insideSkipTake = false;
		public readonly List<LambdaExpression> includeSelectors = new List<LambdaExpression>();
		
		public static Expression Amend(Expression expression)					 
		{
			return new ShiftSubCollectionIncludesOutsideSkipTakeAmender().Visit(expression);
		}

		protected override Expression VisitMethodCall(MethodCallExpression methodCallExpression)
		{
			if (!(methodCallExpression.Method.DeclaringType == typeof(Queryable)
				|| methodCallExpression.Method.DeclaringType == typeof(Enumerable)
				|| methodCallExpression.Method.DeclaringType == typeof(QueryableExtensions)))
			{
				return base.VisitMethodCall(methodCallExpression);
			}

			var saveInsideSkipTake = this.insideSkipTake;

			if (methodCallExpression.Method.Name == "Skip" || methodCallExpression.Method.Name == "Take")
			{
				this.insideSkipTake = true;

				try
				{
					var retval = base.VisitMethodCall(methodCallExpression);

					if (!saveInsideSkipTake)
					{
						foreach (var includeSelector in this.includeSelectors)
						{
							retval = Expression.Call(MethodInfoFastRef.QueryableExtensionsIncludeMethod.MakeGenericMethod(retval.Type.GetGenericArguments()[0], includeSelector.Body.Type), retval, includeSelector);
						}
					}

					return retval;
				}
				finally
				{
					this.insideSkipTake = saveInsideSkipTake;
				}
			}
			else if (methodCallExpression.Method.Name == "Select" && this.insideSkipTake)
			{
				var lambda = methodCallExpression.Arguments[1].StripQuotes();

				var saveSelector = this.selector;

				if (this.selector != null)
				{
					var body = SqlExpressionReplacer.Replace(this.selector.Body, this.selector.Parameters[0], lambda.Body);

					this.selector = Expression.Lambda(body, lambda.Parameters[0]);
				}
				else
				{
					this.selector = lambda;
				}

				try
				{
					return base.VisitMethodCall(methodCallExpression);
				}
				finally
				{
					this.selector = saveSelector;
				}
			}
			else if (methodCallExpression.Method.Name == "Include" && this.insideSkipTake)
			{
				var source = Visit(methodCallExpression.Arguments[0]);

				List<MemberInfo> path;

				if (this.selector == null)
				{
					path = new List<MemberInfo>();
				}
				else
				{
					path = ParameterPathFinder.Find(this.selector);
				}

				if (path == null)
				{
					ParameterPathFinder.Find(this.selector);

					return source;
				}

				bool IsRelatedObjectsMemberExpression(Expression expression)
				{
					return expression is MemberExpression memberExpression
							&& memberExpression.Member.GetMemberReturnType().IsGenericType
							&& memberExpression.Member.GetMemberReturnType().GetGenericTypeDefinition() == typeof(RelatedDataAccessObjects<>);
				}

				var includeSelector = methodCallExpression.Arguments[1].StripQuotes();
				var includesRelatedDataAccessObjects = SqlExpressionFinder.FindExists(includeSelector.Body, IsRelatedObjectsMemberExpression);

				if (includesRelatedDataAccessObjects)
				{
					// Create a new include selector adjusting for any additional member accesses necessary because of select projections

					var oldParam = includeSelector.Parameters[0];
					var oldBody = includeSelector.Body;

					var newParam = path.Count > 0 ? Expression.Parameter(path.First().DeclaringType) : oldParam;

					var replacement = path.Aggregate((Expression)newParam, Expression.MakeMemberAccess, c => c);

					var newBody = SqlExpressionReplacer.Replace(oldBody, oldParam, replacement);

					this.includeSelectors.Add(Expression.Lambda(newBody, newParam));
				}

				return source;
			}

			return base.VisitMethodCall(methodCallExpression);
		}
	}
}
