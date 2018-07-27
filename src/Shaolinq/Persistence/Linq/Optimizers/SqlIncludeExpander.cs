// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System.Linq;
using System.Linq.Expressions;
using Platform;
using Platform.Reflection;
using Shaolinq.TypeBuilding;

namespace Shaolinq.Persistence.Linq.Optimizers
{
	/// <summary>
	/// Converts <c>queryable.Include(c => c.A.B) -> queryable.Select(c => c.IncludeDirect(d => d.A.B))</c>.
	/// </summary>
	/// <remarks>
	/// Also converts nested includes calls into a single include call.
	/// <code>
	/// Include(c => c.Shops.Include(d => d.Toys.Include(e => e.Shop.Mall.Shops2))) -> Include(c => c.Shops.IncludedItems().Toys.IncludedItems().Shop.Mall.Shops2)
	/// </code>
	/// </remarks>
	public class SqlIncludeExpander
		: SqlExpressionVisitor
	{
		private SqlIncludeExpander()
		{
		}

		protected override Expression VisitConstant(ConstantExpression constantExpression)
		{
			if (constantExpression.Value is IQueryable queryable && queryable.Expression != constantExpression)
			{
				return Visit(queryable.Expression);
			}

			return base.VisitConstant(constantExpression);
		}

		public static Expression Expand(Expression expression)
		{
			return new SqlIncludeExpander().Visit(expression);
		}
		
		protected override Expression VisitLambda(LambdaExpression expression)
		{
			var body = Visit(expression.Body);

			if (body == expression.Body)
			{
				return expression;
			}

			if (body.Type != expression.ReturnType)
			{
				if (!expression.ReturnType.IsAssignableFrom(body.Type))
				{
					return Expression.Lambda(body, expression.Parameters);
				}
			}

			return Expression.Lambda(expression.Type, body, expression.Parameters);
		}

		private bool alreadyInsideInclude = false;

		protected override Expression VisitMethodCall(MethodCallExpression methodCallExpression)
		{
			if (methodCallExpression.Method.GetGenericMethodOrRegular() != MethodInfoFastRef.QueryableExtensionsIncludeMethod)
			{
				return base.VisitMethodCall(methodCallExpression);
			}

			var saveAlreadyInsideInclude = this.alreadyInsideInclude;

			this.alreadyInsideInclude = true;

			try
			{
				var parameter = Expression.Parameter(methodCallExpression.Method.GetGenericArguments()[0]);
				var currentIncludeSelector = Visit(methodCallExpression.Arguments[1]).StripQuotes();

				if (methodCallExpression.Arguments[0].Type.GetGenericTypeDefinitionOrNull() == typeof(RelatedDataAccessObjects<>))
				{
					var source = Visit(methodCallExpression.Arguments[0]);
					var includedItems = Expression.Call(QueryableExtensions.IncludedItemsMethod.MakeGenericMethod(source.Type.GetSequenceElementType()), source);

					if (saveAlreadyInsideInclude)
					{
						// Instead of (1) we unwrap and get (2) which ReferencedRelatedObjectPropertyGatherer can process
						// (See: Test_Include_Two_Level_Of_Collections2)
						//
						// Include(c => c.Shops.Include(d => d.Toys.Include(e => e.Shop.Mall.Shops2)))
						//
						// 1) c.Shops.IncludedItems().IncludeDirect(d => d.Toys.IncludedItems().IncludeDirect(e => e.Shop.Mall.Shops2))
						// 2) c.Shops.IncludedItems().Toys.IncludedItems().Shop.Mall.Shops2
						var result = SqlExpressionReplacer.Replace(currentIncludeSelector.Body, currentIncludeSelector.Parameters[0], includedItems);
						
						return result;
					}
					else
					{
						var result = Expression.Call(MethodInfoFastRef.DataAccessObjectExtensionsIncludeMethod.MakeGenericMethod(parameter.Type, currentIncludeSelector.ReturnType), includedItems, currentIncludeSelector);

						return result;
					}
				}
				else
				{
					var body = Expression.Call(MethodInfoFastRef.DataAccessObjectExtensionsIncludeMethod.MakeGenericMethod(parameter.Type, currentIncludeSelector.ReturnType), parameter, currentIncludeSelector);
					var selector = Expression.Lambda(body, parameter);
					var source = Visit(methodCallExpression.Arguments[0]);
					var selectCall = Expression.Call(MethodInfoFastRef.QueryableSelectMethod.MakeGenericMethod(parameter.Type, parameter.Type), source, selector);

					return selectCall;
				}
			}
			finally
			{
				this.alreadyInsideInclude = saveAlreadyInsideInclude;
			}
		}
	}
}
