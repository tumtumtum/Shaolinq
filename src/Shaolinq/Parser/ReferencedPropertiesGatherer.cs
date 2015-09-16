using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using ExpressionVisitor = Platform.Linq.ExpressionVisitor;

namespace Shaolinq.Parser
{
	public class ReferencedPropertiesGatherer
		: ExpressionVisitor
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

		protected override Expression VisitMemberAccess(MemberExpression memberExpression)
		{
			if (memberExpression.Expression == target)
			{
				if (memberExpression.Member is PropertyInfo)
				{
					results.Add((PropertyInfo) memberExpression.Member);
				}
			}
			
			return base.VisitMemberAccess(memberExpression);
		}
	}
}
