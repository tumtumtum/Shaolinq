// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System.Collections.Generic;
using System.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Expressions
{
	public class SqlExpressionHasher
		: SqlExpressionVisitor
	{
		private int hashCode;
		private readonly SqlExpressionComparerOptions options;

		private SqlExpressionHasher(SqlExpressionComparerOptions options)
		{
			this.options = options;
		}

		public static int Hash(Expression expression)
		{
			return Hash(expression, SqlExpressionComparerOptions.None);
		}

		public static int Hash(Expression expression, SqlExpressionComparerOptions options)
		{
			var hasher = new SqlExpressionHasher(options);

			hasher.Visit(expression);

			return hasher.hashCode;
		}

		protected override Expression Visit(Expression expression)
		{
			if (expression != null)
			{
				this.hashCode ^= (int)expression.NodeType << 25;
				this.hashCode ^= expression.Type.GetHashCode();
			}

			return base.Visit(expression);
		}

		protected override MemberBinding VisitBinding(MemberBinding binding)
		{
			this.hashCode ^= (int)binding.BindingType;
			this.hashCode ^= binding.Member.GetHashCode();
			
			return base.VisitBinding(binding);
		}

		protected override ElementInit VisitElementInitializer(ElementInit initializer)
		{
			this.hashCode ^= (int)initializer.Arguments.Count << 16;
			
			return base.VisitElementInitializer(initializer);
		}

		protected override Expression VisitUnary(UnaryExpression unaryExpression)
		{
			if (unaryExpression.Method != null)
			{
				this.hashCode ^= unaryExpression.Method.GetHashCode();
			}

			return base.VisitUnary(unaryExpression);
		}

		protected override Expression VisitBinary(BinaryExpression binaryExpression)
		{
			if (binaryExpression.Method != null)
			{
				this.hashCode ^= binaryExpression.Method.GetHashCode();
			}

			return base.VisitBinary(binaryExpression);
		}

		protected override Expression VisitTypeIs(TypeBinaryExpression expression)
		{
			this.hashCode ^= expression.TypeOperand.GetHashCode();

			return base.VisitTypeIs(expression);
		}

		protected override Expression VisitConstant(ConstantExpression constantExpression)
		{
			var type = constantExpression.Type;

			if ((this.options & SqlExpressionComparerOptions.IgnoreConstants) != 0)
			{
				return constantExpression;
			}

			if (type.IsValueType)
			{
				if (constantExpression.Value != null)
				{
					this.hashCode ^= constantExpression.Value.GetHashCode();
				}
			}
			else if (typeof(Expression).IsAssignableFrom(constantExpression.Type))
			{
				this.Visit((Expression)constantExpression.Value);
			}

			return constantExpression;
		}

		protected override Expression VisitConditional(ConditionalExpression expression)
		{
			return base.VisitConditional(expression);
		}

		protected override Expression VisitParameter(ParameterExpression expression)
		{
			this.hashCode ^= expression.Name?.GetHashCode() ?? 0;

			return base.VisitParameter(expression);
		}

		protected override Expression VisitMemberAccess(MemberExpression memberExpression)
		{
			this.hashCode ^= memberExpression.Member.GetHashCode();

			return base.VisitMemberAccess(memberExpression);
		}

		protected override Expression VisitMethodCall(MethodCallExpression methodCallExpression)
		{
			this.hashCode ^= methodCallExpression.Arguments.Count;

			return base.VisitMethodCall(methodCallExpression);
		}

		protected override IReadOnlyList<T> VisitExpressionList<T>(IReadOnlyList<T> original)
		{
			if (original == null)
			{
				return null;
			}
			
			this.hashCode ^= original.Count;

			foreach (var value in original)
			{
				this.hashCode ^= (int)value.NodeType;
				this.hashCode ^= (int)value.NodeType;
			}

			return base.VisitExpressionList<T>(original);
		}

		protected override IReadOnlyList<Expression> VisitExpressionList(IReadOnlyList<Expression> original)
		{
			return this.VisitExpressionList<Expression>(original);
		}

		protected override MemberAssignment VisitMemberAssignment(MemberAssignment assignment)
		{
			this.hashCode ^= (int)assignment.BindingType;
			this.hashCode ^= assignment.Member.GetHashCode();

			return base.VisitMemberAssignment(assignment);
		}

		protected override MemberMemberBinding VisitMemberMemberBinding(MemberMemberBinding binding)
		{
			if (binding == null)
			{
				return null;
			}

			this.hashCode ^= binding.Bindings.Count;
			this.hashCode ^= binding.Member.GetHashCode();

			return base.VisitMemberMemberBinding(binding);
		}

		protected override MemberListBinding VisitMemberListBinding(MemberListBinding binding)
		{
			if (binding == null)
			{
				return null;
			}

			this.hashCode ^= (int)binding.BindingType;
			this.hashCode ^= binding.Initializers.Count;
			this.hashCode ^= binding.Member.GetHashCode();

			foreach (var value in binding.Initializers)
			{
				this.hashCode ^= value.AddMethod.GetHashCode();
			}

			return base.VisitMemberListBinding(binding);
		}

		protected override IReadOnlyList<MemberBinding> VisitBindingList(IReadOnlyList<MemberBinding> original)
		{
			if (original == null)
			{
				return null;
			}

			this.hashCode ^= original.Count;

			foreach (var value in original)
			{
				this.hashCode ^= (int)value.BindingType;
				this.hashCode ^= value.Member.GetHashCode();
			}

			return base.VisitBindingList(original);
		}

		protected override IReadOnlyList<ElementInit> VisitElementInitializerList(IReadOnlyList<ElementInit> original)
		{
			if (original == null)
			{
				return null;
			}

			this.hashCode ^= original.Count;
			
			return base.VisitElementInitializerList(original);
		}

		protected override Expression VisitLambda(LambdaExpression expression)
		{
			return base.VisitLambda(expression);
		}

		protected override Expression VisitNew(NewExpression expression)
		{
			this.hashCode ^= expression.Arguments.Count;
			this.hashCode ^= expression.Constructor.GetHashCode();

			return base.VisitNew(expression);
		}

		protected override Expression VisitMemberInit(MemberInitExpression expression)
		{
			this.hashCode ^= expression.Bindings.Count << 8;
			
			return base.VisitMemberInit(expression);
		}

		protected override Expression VisitListInit(ListInitExpression expression)
		{
			this.hashCode ^= expression.Initializers.Count;

			return base.VisitListInit(expression);
		}

		protected override Expression VisitNewArray(NewArrayExpression expression)
		{
			this.hashCode ^= expression.Expressions.Count;

			return base.VisitNewArray(expression);
		}

		protected override Expression VisitInvocation(InvocationExpression expression)
		{
			this.hashCode ^= expression.Arguments.Count << 8;
			
			return base.VisitInvocation(expression);
		}

		protected override Expression VisitConstantPlaceholder(SqlConstantPlaceholderExpression constantPlaceholder)
		{
			this.hashCode ^= constantPlaceholder.Index;

			if ((this.options & SqlExpressionComparerOptions.IgnoreConstantPlaceholders) != 0)
			{
				return constantPlaceholder;
			}
			else
			{
				return base.VisitConstantPlaceholder(constantPlaceholder);
			}
		}

		protected override Expression VisitObjectReference(SqlObjectReferenceExpression objectReferenceExpression)
		{
			return base.VisitObjectReference(objectReferenceExpression);
		}

		protected override Expression VisitJoin(SqlJoinExpression join)
		{
			this.hashCode ^= (int)join.JoinType << 16;

			return base.VisitJoin(join);
		}

		protected override Expression VisitTable(SqlTableExpression table)
		{
			this.hashCode ^= table.Name.GetHashCode();
			this.hashCode ^= table.Alias.GetHashCode();
			
			return base.VisitTable(table);
		}

		protected override Expression VisitColumn(SqlColumnExpression columnExpression)
		{
			this.hashCode ^= columnExpression.AliasedName?.GetHashCode() ?? 0;

			return base.VisitColumn(columnExpression);
		}

		protected override Expression VisitFunctionCall(SqlFunctionCallExpression functionCallExpression)
		{
			this.hashCode ^= (int)functionCallExpression.Function << 8;

			return base.VisitFunctionCall(functionCallExpression);
		}

		protected override Expression VisitSubquery(SqlSubqueryExpression subquery)
		{
			return base.VisitSubquery(subquery);
		}

		protected override Expression VisitAggregate(SqlAggregateExpression sqlAggregate)
		{
			this.hashCode ^= (int)sqlAggregate.AggregateType;
			this.hashCode ^= sqlAggregate.IsDistinct ? 1 : 0;

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

			this.hashCode ^= (int)orderByExpression.OrderType;

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

		protected override IReadOnlyList<SqlColumnDeclaration> VisitColumnDeclarations(IReadOnlyList<SqlColumnDeclaration> columns)
		{
			if (columns == null)
			{
				return null;
			}

			this.hashCode ^= columns.Count << 24;

			return base.VisitColumnDeclarations(columns);
		}
		
		protected override Expression VisitQueryArgument(SqlQueryArgumentExpression expression)
		{
			this.hashCode ^= expression.Index;

			return expression;
		}
	}
}
