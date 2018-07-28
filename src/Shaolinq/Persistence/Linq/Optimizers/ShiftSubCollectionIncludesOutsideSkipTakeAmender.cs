using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Platform.Reflection;
using Shaolinq.TypeBuilding;

namespace Shaolinq.Persistence.Linq.Optimizers
{
	/// <summary>
	/// Moves Include statements that include sub collections outside of Skip/Take if possible otherwise eliminate them altogether.
	/// </summary>
	/// <remarks>
	/// <code>
	/// query.Include(c => c.Shops).Skip(1).Take(10)
	/// ->
	/// query.Skip(1).Take(10).Include(c => c.Shops)
	/// </code>
	/// </remarks>
	public class ShiftSubCollectionIncludesOutsideSkipTakeAmender
		: SqlExpressionVisitor
	{
		private bool insideSkipTake = false;
		private List<MemberInfo> memberPath = new List<MemberInfo>();
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
				var path = ParameterPathFinder.Find(methodCallExpression.Arguments[1].StripQuotes());

				if (path == null)
				{
					var saveMemberPath = this.memberPath;

					// Just remove any high up includes

					this.memberPath = null;
					
					try
					{
						return base.VisitMethodCall(methodCallExpression);
					}
					finally
					{
						this.memberPath = saveMemberPath;
					}
				}
				else
				{
					var saveMemberPath = this.memberPath;

					try
					{
						this.memberPath?.AddRange(path);

						return base.VisitMethodCall(methodCallExpression);
					}
					finally
					{
						this.memberPath = saveMemberPath;
					}
				}
			}
			else if (methodCallExpression.Method.Name == "Include" && this.insideSkipTake)
			{
				// Remove the include if it's not possible to include after the select

				if (this.memberPath == null)
				{
					return Visit(methodCallExpression.Arguments[0]);
				}

				// Create a new include selector adjusting for any additional member accesses necessary because
				// of intervening Select statements from the move

				var includeSelector = methodCallExpression.Arguments[1].StripQuotes();
				
				var oldParam = includeSelector.Parameters[0];
				var oldBody = includeSelector.Body;
				
				var newParam = this.memberPath.Count > 0 ? Expression.Parameter(this.memberPath.First().DeclaringType) : oldParam;

				var replacement = this.memberPath.Aggregate((Expression)newParam, Expression.MakeMemberAccess, c => c);

				var newBody = SqlExpressionReplacer.Replace(oldBody, oldParam, replacement);

				this.includeSelectors.Add(Expression.Lambda(newBody, newParam));

				return this.Visit(methodCallExpression.Arguments[0]);
			}

			return base.VisitMethodCall(methodCallExpression);
		}
	}
}
