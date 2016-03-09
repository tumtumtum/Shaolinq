// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using Shaolinq.Persistence.Linq.Expressions;

namespace Shaolinq.Persistence.Linq
{
	public class ProjectorFormatter
		: SqlExpressionVisitor
	{
		private readonly StringBuilder output;

		public ProjectorFormatter()
		{
			this.output = new StringBuilder();
		}

		protected override Expression VisitFunctionCall(SqlFunctionCallExpression functionCallExpression)
		{
			this.output.Append(functionCallExpression.Function);
			this.output.Append("\"");

			foreach (var arg in functionCallExpression.Arguments)
			{
				this.Visit(arg);

				this.output.Append(", ");
			}

			if (functionCallExpression.Arguments.Count > 0)
			{
				this.output.Length -= 2;
			}

			this.output.Append("\"");

			return functionCallExpression;
		}

		protected override Expression VisitBinary(BinaryExpression binaryExpression)
		{
			this.output.Append(binaryExpression.Method.Name);

			this.output.Append("(");

			this.Visit(binaryExpression.Left);
			this.output.Append(", ");
			this.Visit(binaryExpression.Right);

			this.output.Append(")");

			return binaryExpression;
		}

		protected override Expression VisitUnary(UnaryExpression unaryExpression)
		{
			this.output.Append(unaryExpression.Method.Name);
			this.output.Append("(");
			this.output.Append(unaryExpression.Operand);
			this.output.Append(")");

			return unaryExpression;
		}

		protected override Expression VisitColumn(SqlColumnExpression columnExpression)
		{
			this.output.AppendFormat("COLUMN({0}.{1})", columnExpression.SelectAlias, columnExpression.Name);

			return columnExpression;
		}

		protected override MemberAssignment VisitMemberAssignment(MemberAssignment assignment)
		{
			this.output.Append(assignment.Member.Name).Append(" = ");
			this.Visit(assignment.Expression);

			return assignment;
		}

		protected override MemberListBinding VisitMemberListBinding(MemberListBinding binding)
		{
			this.output.Append(binding.Member.Name).Append(" = ");
			this.VisitElementInitializerList(binding.Initializers);

			return binding;
		}

		protected override Expression VisitMemberAccess(MemberExpression memberExpression)
		{
			this.Visit(memberExpression.Expression);
			this.output.Append(".");
			this.output.Append(memberExpression.Member.Name);

			return memberExpression;
		}

		protected override Expression VisitAggregate(SqlAggregateExpression sqlAggregate)
		{
			this.output.Append(sqlAggregate.ToString());

			return sqlAggregate;
		}

		protected override Expression VisitAggregateSubquery(SqlAggregateSubqueryExpression aggregate)
		{
			this.output.Append(aggregate.ToString());

			return aggregate;
		}

		protected override Expression VisitConditional(ConditionalExpression expression)
		{
			this.output.Append("(");
			this.Visit(expression.Test);
			this.output.Append(")");

			this.output.Append(" ? (");
			this.Visit(expression.IfTrue);
			this.output.Append(") : (");
			this.Visit(expression.IfFalse);
			this.output.Append(")");

			return expression;
		}

		protected override Expression VisitConstant(ConstantExpression constantExpression)
		{
			this.output.Append(Convert.ToString(constantExpression.Value));

			return constantExpression;
		}

		protected override Expression VisitConstantPlaceholder(SqlConstantPlaceholderExpression constantPlaceholder)
		{
			this.output.AppendFormat("$${0}", constantPlaceholder.Index);

			return constantPlaceholder;
		}

		protected override ElementInit VisitElementInitializer(ElementInit initializer)
		{
			this.output.Append("(");

			foreach (var element in initializer.Arguments)
			{
				this.Visit(element);
				this.output.Append(", ");
			}

			if (initializer.Arguments.Count > 0)
			{
				this.output.Length -= 2;
			}

			this.output.Append(")");

			return initializer;
		}

		protected override IReadOnlyList<ElementInit> VisitElementInitializerList(IReadOnlyList<ElementInit> original)
		{
			this.output.Append("{");

			foreach (var element in original)
			{
				this.VisitElementInitializer(element);
				this.output.Append(", ");
			}

			if (original.Count > 0)
			{
				this.output.Length -= 2;
			}

			this.output.Append("}");

			return original;
		}

		protected override Expression VisitListInit(ListInitExpression expression)
		{
			return base.VisitListInit(expression);
		}

		protected override Expression VisitNewArray(NewArrayExpression expression)
		{
			return base.VisitNewArray(expression);
		}

		protected override Expression VisitMethodCall(MethodCallExpression methodCallExpression)
		{
			this.output.AppendFormat("{0}.{1}", methodCallExpression.Method.ReflectedType.FullName, methodCallExpression.Method.Name);
			this.output.Append("(");
			foreach (var arg in methodCallExpression.Arguments)
			{
				this.Visit(arg);
				this.output.Append(", ");
			}

			if (methodCallExpression.Arguments.Count > 0)
			{
				this.output.Length -= 2;
			}

			this.output.Append(")");

			return methodCallExpression;
		}

		protected override IReadOnlyList<MemberBinding> VisitBindingList(IReadOnlyList<MemberBinding> original)
		{
			this.output.Append("{");


			foreach (var binding in original)
			{
				this.VisitBinding(binding);
				this.output.Append(", ");
			}

			if (original.Count > 0)
			{
				this.output.Length -= 2;
			}

			this.output.Append("}");

			return original;
		}

		protected override Expression VisitMemberInit(MemberInitExpression expression)
		{
			this.VisitNew(expression.NewExpression);
			this.VisitBindingList(expression.Bindings);

			return expression;
		}

		protected override Expression VisitNew(NewExpression expression)
		{
			this.output.AppendFormat("new {0}", expression.Constructor.ReflectedType.FullName);
			this.output.Append("(");

			foreach (var arg in expression.Arguments)
			{
				this.Visit(arg);
				this.output.Append(", ");
			}

			if (expression.Arguments.Count > 0)
			{
				this.output.Length -= 2;
			}

			this.output.Append(")");

			return base.VisitNew(expression);
		}
	}
}
