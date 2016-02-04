using System.Collections.Generic;
using System.Linq.Expressions;
using Platform;
using Platform.Reflection;
using Shaolinq.TypeBuilding;

namespace Shaolinq.Persistence.Linq.Optimizers
{
	public class SqlIncludeExpressionCollator
		: SqlExpressionVisitor
	{
		private readonly Dictionary<Expression, List<Expression>> includeExpressionsBySource = new Dictionary<Expression, List<Expression>>();

		private SqlIncludeExpressionCollator()
		{
		}

		public static Expression Collate(Expression expression)
		{
			var visitor = new SqlIncludeExpressionCollator();
			var result = visitor.Visit(expression);

			foreach (var keyValue in visitor.includeExpressionsBySource)
			{
				var source = keyValue.Key;
				var expressions = keyValue.Value;

				var newSource = Expression.Call(MethodInfoFastRef.QueryableExtensionsIncludeManyMethod.MakeGenericMethod(source.Type.GetSequenceElementType()), source, Expression.NewArrayInit(typeof(LambdaExpression), expressions));

				result = SqlExpressionReplacer.Replace(result, source, newSource);
			}
			
			return result;
		}

		protected override Expression VisitMethodCall(MethodCallExpression methodCallExpression)
		{
			if (methodCallExpression.Method.GetGenericMethodOrRegular() == MethodInfoFastRef.QueryableExtensionsIncludeMethod)
			{
				List<Expression> expressions;
				var source = base.Visit(methodCallExpression.Arguments[0]);
				var currentIncludeSelector = this.Visit(methodCallExpression.Arguments[1]).StripQuotes();

				if (!includeExpressionsBySource.TryGetValue(source, out expressions))
				{
					expressions = new List<Expression>();
					includeExpressionsBySource[source] = expressions;
				}

				expressions.Add(currentIncludeSelector);

				return source;
			}

			return base.VisitMethodCall(methodCallExpression);
		}
	}
}
