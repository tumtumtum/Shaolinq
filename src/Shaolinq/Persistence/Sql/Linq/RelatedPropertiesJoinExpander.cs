using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Platform;
using Shaolinq.Persistence.Sql.Linq.Expressions;
using Shaolinq.Persistence.Sql.Linq.Optimizer;
using Shaolinq.TypeBuilding;

namespace Shaolinq.Persistence.Sql.Linq
{
	public class RelatedPropertiesJoinExpander
		: SqlExpressionVisitor
	{
		private readonly BaseDataAccessModel model;

		private RelatedPropertiesJoinExpander(BaseDataAccessModel model)
		{
			this.model = model;
		}

		public static Expression Expand(BaseDataAccessModel model, Expression expression)
		{
			return new RelatedPropertiesJoinExpander(model).Visit(expression);
		}

		protected override Expression VisitMethodCall(MethodCallExpression methodCallExpression)
		{
			if (methodCallExpression.Method.DeclaringType == typeof(Queryable)
			    || methodCallExpression.Method.DeclaringType == typeof(Enumerable)
			    || methodCallExpression.Method.DeclaringType == typeof(QueryableExtensions))
			{
				switch (methodCallExpression.Method.Name)
				{
					case "Where":
						return this.RewriteBasicProjection(methodCallExpression);
				}
			}

			return base.VisitMethodCall(methodCallExpression);
		}

		private static Expression MakeSelectorForType(Type type, MemberExpression key)
		{
			Expression body = null;
			var parameter = Expression.Parameter(type);
			
			if (key != null)
			{
				body = Expression.Property(parameter, ((PropertyInfo)key.Member));
			}
			else
			{
				body = parameter;
			}

			return Expression.Lambda(body, parameter);
		}

		private static LambdaExpression MakeJoinProjector(Type leftType, Type rightType)
		{
			var leftParameter = Expression.Parameter(leftType);
			var rightParameter = Expression.Parameter(rightType);
			var newExpression = Expression.New(typeof(Pair<,>).MakeGenericType(leftType, rightType));

			var body = Expression.MemberInit(newExpression, 
				Expression.Bind(newExpression.Type.GetProperty("Left", BindingFlags.Public | BindingFlags.Instance), leftParameter),
				Expression.Bind(newExpression.Type.GetProperty("Right", BindingFlags.Public | BindingFlags.Instance), rightParameter));

			return Expression.Lambda(body, leftParameter, rightParameter);
		}
		
		private static MethodCallExpression MakeJoinCallExpression(Expression left, Expression right, MemberExpression key)
		{
			var leftElementType = left.Type.GetGenericArguments()[0];
			var rightElementType = right.Type.GetGenericArguments()[0];

			var leftSelector = MakeSelectorForType(leftElementType, key);
			var rightSelector = MakeSelectorForType(rightElementType, null);
			var projector = MakeJoinProjector(leftElementType, rightElementType);

			var method = MethodInfoFastRef.QueryableJoinMethod.MakeGenericMethod(leftElementType, rightElementType,  key.Type, projector.ReturnType);

			right = Expression.Call(null, MethodInfoFastRef.QueryableDefaultIfEmptyMethod.MakeGenericMethod(rightElementType), right);

			return Expression.Call(null, method, left, right, Expression.Quote(leftSelector), Expression.Quote(rightSelector), Expression.Quote(projector));
		}

		protected Expression RewriteBasicProjection(MethodCallExpression methodCallExpression)
		{
			var source = methodCallExpression.Arguments[0];
			var predicateOrSelector = (LambdaExpression)QueryBinder.StripQuotes(methodCallExpression.Arguments[1]);

			var sourceParameterExpression = predicateOrSelector.Parameters[0];

			var memberAccessExpressionsNeedingJoins = ReferencedRelatedObjectPropertyGatherer.Gather(this.model, predicateOrSelector, sourceParameterExpression);

			if (memberAccessExpressionsNeedingJoins.Count > 0)
			{
				var memberExpression = memberAccessExpressionsNeedingJoins[0];
				var memberMemberExpression = (MemberExpression)memberExpression.Expression;
				
				var left = source;
				var method = this.model.GetType().GetMethod("GetDataAccessObjects").MakeGenericMethod(memberMemberExpression.Type);

				var right = Expression.Constant(method.Invoke(this.model, null));
				var joinExpression = MakeJoinCallExpression(left, right, memberMemberExpression);

				var newParameter = Expression.Parameter(joinExpression.Method.ReturnType.GetGenericArguments()[0]);
				var newBody = ExpressionReplacer.Replace(predicateOrSelector.Body, memberExpression, Expression.Property(Expression.Property(newParameter, "Right"), memberExpression.Member.Name));
				newBody = ExpressionReplacer.Replace(newBody, sourceParameterExpression, Expression.Property(newParameter, "Left"));
				var newMethod = methodCallExpression.Method.GetGenericMethodDefinition().MakeGenericMethod(newParameter.Type);

				var newPredicateOrSelector = Expression.Lambda(newBody, newParameter);

				var newCall = Expression.Call(methodCallExpression.Object, newMethod, new Expression[] { joinExpression, newPredicateOrSelector });

				var newSelectParameter = Expression.Parameter(newParameter.Type);
				var newSelectBody = Expression.Property(newSelectParameter, "Left");
				var newSelect = Expression.Lambda(newSelectBody, newSelectParameter);

				var newSelectMethod = MethodInfoFastRef.QueryableSelectMethod.MakeGenericMethod(newParameter.Type, newSelect.ReturnType);

				return Expression.Call(null, newSelectMethod, new Expression[] { newCall, newSelect });
			}

			return Expression.Call(null, methodCallExpression.Method, new[] { source, predicateOrSelector });
		}
	}
}