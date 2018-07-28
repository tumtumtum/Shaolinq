// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Shaolinq.Persistence.Linq.Optimizers
{
	/// <summary>
	/// Finds the property/field path required to get to the parameter of a selector.
	/// </summary>
	/// <remarks>
	/// <code>c => new { a = new { b = c } } -> [member(a), member(b)] </code>
	/// </remarks>
	public class ParameterPathFinder
		: SqlExpressionVisitor
	{
		private List<MemberInfo> result;
		private readonly ParameterExpression parameter;
		private readonly Stack<MemberInfo> pathStack = new Stack<MemberInfo>();
		
		public ParameterPathFinder(ParameterExpression parameter)
		{
			this.parameter = parameter;
		}

		private static bool IsAnonymousType(Type type)
		{
			var hasCompilerGeneratedAttribute = type.GetCustomAttributes(typeof(CompilerGeneratedAttribute), false).Any();
			var nameContainsAnonymousType = type.FullName.Contains("AnonymousType");

			return hasCompilerGeneratedAttribute && nameContainsAnonymousType;
		}

		public static List<MemberInfo> Find(LambdaExpression lambdaExpression)
		{
			var finder = new ParameterPathFinder(lambdaExpression.Parameters[0]);
				
			finder.Visit(lambdaExpression.Body);
				
			return finder.result;
		}

		protected override Expression VisitParameter(ParameterExpression expression)
		{
			if (expression == this.parameter)
			{
				if (this.result == null)
				{
					this.result = this.pathStack.ToList();
					this.result.Reverse();

					return expression;
				}
			}

			return base.VisitParameter(expression);
		}

		protected override Expression VisitMethodCall(MethodCallExpression methodCallExpression)
		{
			return methodCallExpression;
		}

		protected override Expression VisitMemberAccess(MemberExpression memberExpression)
		{
			var source = memberExpression.Expression.StripConvert();

			if (source is NewExpression newExpression && IsAnonymousType(newExpression.Type))
			{
				var param = newExpression.Constructor.GetParameters().First(c => c.Name == memberExpression.Member.Name);

				source = newExpression.Arguments[param.Position];

				Visit(source);
			}

			return memberExpression;
		}

		protected override Expression VisitNew(NewExpression expression)
		{
			if (IsAnonymousType(expression.Type))
			{
				var i = 0;

				foreach (var arg in expression.Arguments)
				{
					this.pathStack.Push(expression.Type.GetProperty(expression.Constructor.GetParameters()[i].Name));
					Visit(arg);
					this.pathStack.Pop();

					i++;
				}

				return expression;
			}

			return base.VisitNew(expression);
		}
	}
}