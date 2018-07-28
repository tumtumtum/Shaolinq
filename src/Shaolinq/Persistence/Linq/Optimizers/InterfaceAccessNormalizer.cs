// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)

using System.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Optimizers
{
	public class InterfaceAccessNormalizer
		: SqlExpressionVisitor
	{
		private readonly TypeDescriptorProvider typeDescriptorProvider;

		private InterfaceAccessNormalizer(TypeDescriptorProvider typeDescriptorProvider)
		{
			this.typeDescriptorProvider = typeDescriptorProvider;
		}

		public static Expression Normalize(TypeDescriptorProvider typeDescriptorProvider, Expression expression)
		{
			return new InterfaceAccessNormalizer(typeDescriptorProvider).Visit(expression);
		}

		protected override Expression VisitMemberAccess(MemberExpression memberExpression)
		{
			var expression = Visit(memberExpression.Expression);

			if (expression != null)
			{
				// Check if call is made to an interface property rather than DAO virtual property
				if (memberExpression.Member.DeclaringType != expression.Type && expression.Type.IsDataAccessObjectType())
				{
					var typeDescriptor = this.typeDescriptorProvider.GetTypeDescriptor(expression.Type);
					var member = typeDescriptor?.GetPropertyDescriptorByPropertyName(memberExpression.Member.Name).PropertyInfo;

					if (memberExpression != null)
					{
						return Expression.MakeMemberAccess(expression, member);
					}
				}
			}

			if (expression == memberExpression.Expression)
			{
				return memberExpression;
			}
			else
			{
				return Expression.MakeMemberAccess(expression, memberExpression.Member);
			}
		}

		protected override Expression VisitUnary(UnaryExpression unaryExpression)
		{
			if (unaryExpression.NodeType == ExpressionType.Convert
				&& unaryExpression.Operand.Type.IsDataAccessObjectType()
				&& unaryExpression.Type.IsAssignableFrom(unaryExpression.Operand.Type))
			{
				return Visit(unaryExpression.Operand);
			}

			return base.VisitUnary(unaryExpression);
		}
	}
}