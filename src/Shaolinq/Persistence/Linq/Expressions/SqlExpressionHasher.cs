// Copyright (c) 2007-2014 Thong Nguyen (tumtumtum@gmail.com)

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Expressions
{
	public class SqlExpressionHasher
		: SqlExpressionVisitor
	{
		private int hashCode;
		private bool ignoreConstantPlaceholderValues;

		public static int Hash(Expression expression)
		{
			return Hash(expression, false);
		}

		public static int Hash(Expression expression, bool ignoreConstantPlaceholderValues)
		{
			var hasher = new SqlExpressionHasher
			{
				ignoreConstantPlaceholderValues = ignoreConstantPlaceholderValues
			};

			hasher.Visit(expression);

			return hasher.hashCode;
		}

		protected override Expression Visit(Expression expression)
		{
			if (expression != null)
			{
				hashCode ^= (int)expression.NodeType << 24;
			}

			return base.Visit(expression);
		}

		protected override MemberBinding VisitBinding(MemberBinding binding)
		{
			hashCode ^= (int)binding.BindingType;
			hashCode ^= binding.Member.GetHashCode();
			
			return base.VisitBinding(binding);
		}

		protected override ElementInit VisitElementInitializer(ElementInit initializer)
		{
			hashCode ^= (int)initializer.Arguments.Count << 16;
			
			return base.VisitElementInitializer(initializer);
		}

		protected override Expression VisitUnary(UnaryExpression unaryExpression)
		{
			if (unaryExpression.Method != null)
			{
				hashCode ^= unaryExpression.Method.GetHashCode();
			}

			return base.VisitUnary(unaryExpression);
		}

		protected override Expression VisitBinary(BinaryExpression binaryExpression)
		{
			if (binaryExpression.Method != null)
			{
				hashCode ^= binaryExpression.Method.GetHashCode();
			}

			return base.VisitBinary(binaryExpression);
		}

		protected override Expression VisitTypeIs(TypeBinaryExpression expression)
		{
			hashCode ^= expression.TypeOperand.GetHashCode();

			return base.VisitTypeIs(expression);
		}

		protected override Expression VisitConstant(ConstantExpression constantExpression)
		{
			var type = constantExpression.Type;

			hashCode ^= type.GetHashCode();

			if (type.IsValueType)
			{
				if (constantExpression.Value != null)
				{
					hashCode ^= constantExpression.Value.GetHashCode();
				}
			}

			return base.VisitConstant(constantExpression);
		}

		protected override Expression VisitConditional(ConditionalExpression expression)
		{
			return base.VisitConditional(expression);
		}

		protected override Expression VisitParameter(ParameterExpression expression)
		{
			hashCode ^= expression.Name == null ? 0 : expression.Name.GetHashCode();

			return base.VisitParameter(expression);
		}

		protected override Expression VisitMemberAccess(MemberExpression memberExpression)
		{
			hashCode ^= memberExpression.Member.GetHashCode();

			return base.VisitMemberAccess(memberExpression);
		}

		protected override Expression VisitMethodCall(MethodCallExpression methodCallExpression)
		{
			hashCode ^= methodCallExpression.Arguments.Count;

			return base.VisitMethodCall(methodCallExpression);
		}

		protected override ReadOnlyCollection<Expression> VisitExpressionList(ReadOnlyCollection<Expression> original)
		{
			if (original == null)
			{
				return original;
			}

			hashCode ^= original.Count;

			return base.VisitExpressionList(original);
		}

		protected override MemberAssignment VisitMemberAssignment(MemberAssignment assignment)
		{
			hashCode ^= (int)assignment.BindingType;
			hashCode ^= assignment.Member.GetHashCode();

			return base.VisitMemberAssignment(assignment);
		}

		protected override MemberMemberBinding VisitMemberMemberBinding(MemberMemberBinding binding)
		{
			if (binding == null)
			{
				return binding;
			}

			hashCode ^= binding.Bindings.Count;
			hashCode ^= binding.Member.GetHashCode();

			return base.VisitMemberMemberBinding(binding);
		}

		protected override MemberListBinding VisitMemberListBinding(MemberListBinding binding)
		{
			if (binding == null)
			{
				return binding;
			}

			hashCode ^= (int)binding.BindingType;
			hashCode ^= binding.Initializers.Count;
			hashCode ^= binding.Member.GetHashCode();

			return base.VisitMemberListBinding(binding);
		}

		protected override IEnumerable<MemberBinding> VisitBindingList(ReadOnlyCollection<MemberBinding> original)
		{
			if (original == null)
			{
				return original;
			}

			hashCode ^= original.Count;

			return base.VisitBindingList(original);
		}

		protected override IEnumerable<ElementInit> VisitElementInitializerList(ReadOnlyCollection<ElementInit> original)
		{
			if (original == null)
			{
				return original;
			}

			hashCode ^= original.Count;

			return base.VisitElementInitializerList(original);
		}

		protected override Expression VisitLambda(LambdaExpression expression)
		{
			return base.VisitLambda(expression);
		}

		protected override NewExpression VisitNew(NewExpression expression)
		{
			hashCode ^= expression.Arguments.Count;
			
			return base.VisitNew(expression);
		}

		protected override Expression VisitMemberInit(MemberInitExpression expression)
		{
			hashCode ^= expression.Bindings.Count << 8;
			
			return base.VisitMemberInit(expression);
		}

		protected override Expression VisitListInit(ListInitExpression expression)
		{
			hashCode ^= expression.Initializers.Count;

			return base.VisitListInit(expression);
		}

		protected override Expression VisitNewArray(NewArrayExpression expression)
		{
			hashCode ^= expression.Expressions.Count;

			return base.VisitNewArray(expression);
		}

		protected override Expression VisitInvocation(InvocationExpression expression)
		{
			hashCode ^= expression.Arguments.Count << 8;

			return base.VisitInvocation(expression);
		}

		protected override Expression VisitConstantPlaceholder(SqlConstantPlaceholderExpression constantPlaceholder)
		{
			hashCode ^= constantPlaceholder.Index;

			if (ignoreConstantPlaceholderValues)
			{
				return constantPlaceholder;
			}
			else
			{
				return base.VisitConstantPlaceholder(constantPlaceholder);
			}
		}

		protected override Expression VisitObjectOperand(SqlObjectOperand objectOperand)
		{
			return base.VisitObjectOperand(objectOperand);
		}

		protected override Expression VisitJoin(SqlJoinExpression join)
		{
			hashCode ^= ((int)join.JoinType) << 16;

			return base.VisitJoin(join);
		}

		protected override Expression VisitTable(SqlTableExpression table)
		{
			return base.VisitTable(table);
		}

		protected override Expression VisitColumn(SqlColumnExpression columnExpression)
		{
			return base.VisitColumn(columnExpression);
		}

		protected override Expression VisitFunctionCall(SqlFunctionCallExpression functionCallExpression)
		{
			hashCode ^= (int)functionCallExpression.Function << 24;

			return base.VisitFunctionCall(functionCallExpression);
		}

		protected override Expression VisitSubquery(SqlSubqueryExpression subquery)
		{
			return base.VisitSubquery(subquery);
		}

		protected override Expression VisitAggregate(SqlAggregateExpression sqlAggregate)
		{
			return base.VisitAggregate(sqlAggregate);
		}

		protected override Expression VisitAggregateSubquery(SqlAggregateSubqueryExpression aggregate)
		{
			return base.VisitAggregateSubquery(aggregate);
		}

		protected override Expression VisitSelect(SqlSelectExpression selectExpression)
		{
			return base.VisitSelect(selectExpression);
		}

		protected override Expression VisitOrderBy(SqlOrderByExpression orderByExpression)
		{
			if (orderByExpression == null)
			{
				return null;
			}

			hashCode ^= (int)orderByExpression.OrderType;

			this.Visit(orderByExpression.Expression);

			return base.VisitOrderBy(orderByExpression);
		}

		protected override Expression VisitSource(Expression source)
		{
			return base.VisitSource(source);
		}

		protected override Expression VisitProjection(SqlProjectionExpression projection)
		{
			return base.VisitProjection(projection);
		}

		protected override ReadOnlyCollection<SqlColumnDeclaration> VisitColumnDeclarations(ReadOnlyCollection<SqlColumnDeclaration> columns)
		{
			if (columns == null)
			{
				return null;
			}

			hashCode ^= columns.Count << 8;

			return base.VisitColumnDeclarations(columns);
		}

		protected override Expression VisitDelete(SqlDeleteExpression deleteExpression)
		{
			return base.VisitDelete(deleteExpression);
		}
	}
}
