// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace Shaolinq.Persistence.Computed
{
	public class ReferencedPropertiesGatherer
		: Platform.Linq.ExpressionVisitor
	{
		private readonly Expression target;
		private readonly List<PropertyInfo> results = new List<PropertyInfo>();

		public ReferencedPropertiesGatherer(Expression target)
		{
			this.target = target;
		}

		public static List<PropertyInfo> Gather(Expression expression, Expression target)
		{
			var gatherer = new ReferencedPropertiesGatherer(target);

			gatherer.Visit(expression);

			return gatherer.results;
		}

        protected override Expression VisitMethodCall(MethodCallExpression methodCallExpression)
        {
            if (methodCallExpression.Object == this.target && this.target != null)
            {
                var attributes = methodCallExpression.Method.GetCustomAttributes(typeof(DependsOnPropertyAttribute), true);

                foreach (DependsOnPropertyAttribute attribute in attributes)
                {
                    var property = this.target.Type.GetProperty(attribute.PropertyName);

                    this.results.Add(property);
                }
            }

            return base.VisitMethodCall(methodCallExpression);
        }

        protected override Expression VisitMemberAccess(MemberExpression memberExpression)
		{
			if (memberExpression.Expression == this.target)
			{
			    var info = memberExpression.Member as PropertyInfo;

                if (info != null)
				{
					this.results.Add(info);
				}
			}

		    return base.VisitMemberAccess(memberExpression);
		}
	}
}
