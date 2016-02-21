using System.Linq.Expressions;
using System.Reflection;

namespace Shaolinq.Persistence.Linq.Optimizers
{
	public class MemberAccessReplacer
		: SqlExpressionVisitor
	{
		private readonly MemberInfo member;
		private readonly Expression replacement;

		public MemberAccessReplacer(MemberInfo member, Expression replacement)
		{
			this.member = member;
			this.replacement = replacement;
		}

		public static Expression Replace(Expression expression, MemberInfo member, Expression replacement)
		{
			return new MemberAccessReplacer(member, replacement).Visit(expression);
		}

		protected override Expression VisitMemberAccess(MemberExpression memberAccess)
		{
			if (memberAccess.Member == member)
			{
				return replacement;
			}

			return base.VisitMemberAccess(memberAccess);
		}
	}
}
