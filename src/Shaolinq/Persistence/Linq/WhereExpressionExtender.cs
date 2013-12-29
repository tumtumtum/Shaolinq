// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

﻿using System;
using System.Linq;
using System.Linq.Expressions;
using Platform;
using Shaolinq.TypeBuilding;

namespace Shaolinq.Persistence.Linq
{
	public class WhereExpressionExtender
		: Platform.Linq.ExpressionVisitor
	{
		private readonly Type type;
		private readonly Expression condition;

		private WhereExpressionExtender(Expression condition, Type type)
		{
			this.type = type;
			this.condition = condition;
		}

		public static Expression Extend(Expression expression, Type type, LambdaExpression condition)
		{
			return new WhereExpressionExtender(condition, type).Visit(expression);
		}

		protected override Expression VisitMethodCall(MethodCallExpression methodCallExpression)
		{
			if (methodCallExpression.Method.DeclaringType == typeof(Queryable) || methodCallExpression.Method.DeclaringType == typeof(Enumerable))
			{
				if (TypeHelper.GetElementType(methodCallExpression.Type) == type)
				{
					switch (methodCallExpression.Method.Name)
					{
						case "Where":
						{
							var selectSource = methodCallExpression.Arguments[0];
							var selectCondition = (LambdaExpression)QueryBinder.StripQuotes(methodCallExpression.Arguments[1]);

							if (selectSource.NodeType == ExpressionType.Constant)
							{
								if (typeof(RelatedDataAccessObjects<>).IsAssignableFromIgnoreGenericParameters(((ConstantExpression)selectSource).Value.GetType()))
								{
									var arguments = new Expression[2];
									Expression newSelectSource = null;
									var newSelectArguments = new Expression[2];

									newSelectArguments[0] = selectSource;
									newSelectArguments[1] = this.condition;

									newSelectSource = Expression.Call(null, MethodInfoFastRef.QueryableWhereMethod.MakeGenericMethod(this.type), newSelectArguments);

									arguments[0] = newSelectSource;
									arguments[1] = selectCondition;
                                    
									return Expression.Call(null, methodCallExpression.Method, arguments);
								}
							}

							return methodCallExpression;
						}
						case "Select":
						{
							var selectSource = methodCallExpression.Arguments[0];
							var selectSelector = (LambdaExpression)QueryBinder.StripQuotes(methodCallExpression.Arguments[1]);

							return Expression.Call(null, MethodInfoFastRef.QueryableWhereMethod.MakeGenericMethod(this.type), methodCallExpression, this.condition);
						}
					}
				}
			}

			return base.VisitMethodCall(methodCallExpression);
		}
	}
}
