// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Platform;
using Platform.Reflection;
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
					case "Select":
					case "WhereForUpdate":
					case "SelectForUpdate":
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
		
		private static MethodCallExpression MakeJoinCallExpression(int index, Expression left, Expression right, MemberInfo member)
		{
			var leftElementType = left.Type.GetGenericArguments()[0];
			var rightElementType = right.Type.GetGenericArguments()[0];

			ParameterExpression parameter;
			Expression leftObject = null;

			if (index > 0)
			{
				parameter = Expression.Parameter(leftElementType);
				
				leftObject = CreateReplacementMemberAccessExpression(index - 1, parameter, null);
			}
			else
			{
				parameter = Expression.Parameter(leftElementType);

				leftObject = parameter;
			}

			var leftSelector = Expression.Lambda(Expression.Property(leftObject, member.Name), parameter);

			parameter = Expression.Parameter(rightElementType);
			var rightSelector = Expression.Lambda(parameter, parameter);

			var projector = MakeJoinProjector(leftElementType, rightElementType);

			var method = MethodInfoFastRef.QueryableJoinMethod.MakeGenericMethod(leftElementType, rightElementType, member.GetMemberReturnType(), projector.ReturnType);

			right = Expression.Call(null, MethodInfoFastRef.QueryableDefaultIfEmptyMethod.MakeGenericMethod(rightElementType), right);

			return Expression.Call(null, method, left, right, Expression.Quote(leftSelector), Expression.Quote(rightSelector), Expression.Quote(projector));
		}

		private static Type CreateFinalTupleType(Type previousType, IEnumerable<MemberInfo> membersSortedByName)
		{
			var retval = previousType;

			foreach (var memberInfo in membersSortedByName)
			{
				retval = typeof(Pair<,>).MakeGenericType(retval, memberInfo.GetMemberReturnType().IsDataAccessObjectType() ? memberInfo.GetMemberReturnType() : memberInfo.GetMemberReturnType().GetGenericArguments()[0]);
			}

			return retval;
		}

		private static Expression CreateReplacementMemberAccessExpression(int index, ParameterExpression parameterExpression, string memberName = null)
		{
			Expression retval = parameterExpression;

			for (var i = 0; i <= index; i++)
			{
				if (i == index && memberName != null)
				{
					retval = Expression.Property(Expression.Property(retval, "Right"), memberName);
				
					break;
				}
				
				retval = Expression.Property(retval, "Left");
			}

			return retval;
		}

		private static Expression CreateRightJoinTargetExpression(int index, ParameterExpression parameterExpression)
		{
			Expression retval = parameterExpression;

			for (var i = 0; i <= index - 1; i++)
			{
				retval = Expression.Property(retval, "Left");
			}

			retval = Expression.Property(retval, "Right");

			return retval;
		}

		private static LambdaExpression CreateReplacementPredicateOrSelector(Type finalTupleType, Expression body, List<MemberInfo> membersSortedByName, Dictionary<MemberInfo, IGrouping<MemberInfo, MemberExpression>> expressionsGroupedByMember, ParameterExpression sourceParameterExpression)
		{
			var newParameter = Expression.Parameter(finalTupleType);
			var retval = body;

			var index = membersSortedByName.Count - 1;

			foreach (var member in membersSortedByName)
			{
				foreach (var memberExpression in expressionsGroupedByMember[member])
				{
					if (memberExpression.Expression == sourceParameterExpression)
					{
						var replacementExpression = CreateRightJoinTargetExpression(index, newParameter);

						retval = ExpressionReplacer.Replace(retval, memberExpression, replacementExpression);
					}
					else
					{
						var replacementExpression = CreateReplacementMemberAccessExpression(index, newParameter, memberExpression.Member.Name);

						retval = ExpressionReplacer.Replace(retval, memberExpression, replacementExpression);
					}
				}

				index--;
			}

			retval = ExpressionReplacer.Replace(retval, sourceParameterExpression, CreateReplacementMemberAccessExpression(membersSortedByName.Count - 1, newParameter, null));

			return Expression.Lambda(retval, newParameter);
		}

		protected Expression RewriteBasicProjection(MethodCallExpression methodCallExpression)
		{
			var source = methodCallExpression.Arguments[0];
			var predicateOrSelector = (LambdaExpression)QueryBinder.StripQuotes(methodCallExpression.Arguments[1]);
			var sourceParameterExpression = predicateOrSelector.Parameters[0];
			var memberAccessExpressionsNeedingJoins = ReferencedRelatedObjectPropertyGatherer.Gather(this.model, predicateOrSelector, sourceParameterExpression);
		
			if (memberAccessExpressionsNeedingJoins.Count > 0)
			{
				var expressionsGroupedByMember = memberAccessExpressionsNeedingJoins.GroupBy(c => c.Expression == sourceParameterExpression ? c.Member : ((MemberExpression)c.Expression).Member).ToDictionary(c => c.Key);
				var membersSortedByName = expressionsGroupedByMember.Keys.Sorted((x, y) => String.CompareOrdinal(x.Name, y.Name)).ToList();

				var finalTupleType = CreateFinalTupleType(source.Type.GetGenericArguments()[0], membersSortedByName);

				var newPredicateOrSelector = CreateReplacementPredicateOrSelector(finalTupleType, predicateOrSelector.Body, membersSortedByName, expressionsGroupedByMember, sourceParameterExpression);

				var index = 0;
				var currentLeft = source;

				foreach (var member in membersSortedByName)
				{
					var right = Expression.Constant(null, typeof(RelatedDataAccessObjects<>).MakeGenericType(member.GetMemberReturnType()));

					var join = MakeJoinCallExpression(index, currentLeft, right, member);

					currentLeft = join;
					index++;
				}

				MethodInfo newMethod;
				var newParameterType = newPredicateOrSelector.Parameters[0].Type;

				if (methodCallExpression.Method.Name.StartsWith("Select"))
				{
					var projectionResultType = methodCallExpression.Method.ReturnType.GetGenericArguments()[0];

					newMethod = methodCallExpression.Method.GetGenericMethodDefinition().MakeGenericMethod(newParameterType, projectionResultType);
				}
				else
				{
					newMethod = methodCallExpression.Method.GetGenericMethodDefinition().MakeGenericMethod(newParameterType);
				}

				var newCall = Expression.Call(null, newMethod, new[]
				{
					currentLeft,
					newPredicateOrSelector
				});

				if (newCall.Method.ReturnType != methodCallExpression.Method.ReturnType)
				{
					var selectParameter = Expression.Parameter(newParameterType);
					var selectBody = CreateReplacementMemberAccessExpression(membersSortedByName.Count - 1, selectParameter, null);
					var selectCall = Expression.Lambda(selectBody, selectParameter);

					var selectMethod = MethodInfoFastRef.QueryableSelectMethod.MakeGenericMethod(selectParameter.Type, selectCall.ReturnType);

					newCall = Expression.Call(null, selectMethod, new Expression[] { newCall, selectCall });
				}

				return newCall;
			}

			return methodCallExpression;
		}
	}
}
