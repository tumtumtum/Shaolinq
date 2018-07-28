using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Shaolinq.Persistence.Linq.Optimizers
{
	public class ParameterPathFinder
		: Platform.Linq.ExpressionVisitor
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
					this.Visit(arg);
					this.pathStack.Pop();

					i++;
				}

				return expression;
			}

			return base.VisitNew(expression);
		}

		protected override MemberAssignment VisitMemberAssignment(MemberAssignment assignment)
		{
			this.pathStack.Push(assignment.Member);

			try
			{
				return base.VisitMemberAssignment(assignment);
			}
			finally
			{
				this.pathStack.Pop();
			}
		}
	}
}