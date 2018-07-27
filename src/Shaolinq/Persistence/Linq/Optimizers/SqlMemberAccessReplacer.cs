// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System.Linq.Expressions;
using System.Reflection;

namespace Shaolinq.Persistence.Linq.Optimizers
{
	public class SqlMemberAccessReplacer
		: SqlExpressionVisitor
	{
		private readonly MemberInfo member;
		private readonly Expression replacement;

		public SqlMemberAccessReplacer(MemberInfo member, Expression replacement)
		{
			this.member = member;
			this.replacement = replacement;
		}

		public static Expression Replace(Expression expression, MemberInfo member, Expression replacement)
		{
			return new SqlMemberAccessReplacer(member, replacement).Visit(expression);
		}

		protected override Expression VisitMemberAccess(MemberExpression memberAccess)
		{
			if (memberAccess.Member == this.member)
			{
				return this.replacement;
			}

			return base.VisitMemberAccess(memberAccess);
		}
	}
}
