// Copyright (c) 2007-2014 Thong Nguyen (tumtumtum@gmail.com)

﻿using System;
﻿using System.Collections.Generic;
﻿using System.Collections.ObjectModel;
﻿using System.Linq.Expressions;
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
			output = new StringBuilder();
		}

		protected override Expression VisitFunctionCall(SqlFunctionCallExpression functionCallExpression)
		{
			output.Append(functionCallExpression.Function);
			output.Append("\"");

			foreach (var arg in functionCallExpression.Arguments)
			{
				base.Visit(arg);

				output.Append(", ");
			}

			if (functionCallExpression.Arguments.Count > 0)
			{
				output.Length -= 2;
			}

			output.Append("\"");

			return functionCallExpression;
		}

		protected override Expression VisitBinary(BinaryExpression binaryExpression)
		{
			output.Append(binaryExpression.Method.Name);

			output.Append("(");

			Visit(binaryExpression.Left);
			output.Append(", ");
			Visit(binaryExpression.Right);

			output.Append(")");

			return binaryExpression;
		}

		protected override Expression VisitUnary(UnaryExpression unaryExpression)
		{
			output.Append(unaryExpression.Method.Name);
			output.Append("(");
			output.Append(unaryExpression.Operand);
			output.Append(")");

			return unaryExpression;
		}

		protected override Expression VisitColumn(SqlColumnExpression columnExpression)
		{
			output.AppendFormat("COLUMN({0}.{1})", columnExpression.SelectAlias, columnExpression.Name);

			return columnExpression;
		}

		protected override MemberAssignment VisitMemberAssignment(MemberAssignment assignment)
		{
			output.Append(assignment.Member.Name).Append(" = ");
			Visit(assignment.Expression);

			return assignment;
		}

		protected override MemberListBinding VisitMemberListBinding(MemberListBinding binding)
		{
			output.Append(binding.Member.Name).Append(" = ");
			VisitElementInitializerList(binding.Initializers);

			return binding;
		}

		protected override Expression VisitMemberAccess(MemberExpression memberExpression)
		{
			Visit(memberExpression.Expression);
			output.Append(".");
			output.Append(memberExpression.Member.Name);

			return memberExpression;
		}

		protected override Expression VisitAggregate(SqlAggregateExpression sqlAggregate)
		{
			output.Append(sqlAggregate.ToString());

			return sqlAggregate;
		}

		protected override Expression VisitAggregateSubquery(SqlAggregateSubqueryExpression aggregate)
		{
			output.Append(aggregate.ToString());

			return aggregate;
		}

		protected override Expression VisitConditional(ConditionalExpression expression)
		{
			output.Append("(");
			Visit(expression.Test);
			output.Append(")");

			output.Append(" ? (");
			Visit(expression.IfTrue);
			output.Append(") : (");
			Visit(expression.IfFalse);
			output.Append(")");

			return expression;
		}

		protected override Expression VisitConstant(ConstantExpression constantExpression)
		{
			output.Append(Convert.ToString(constantExpression.Value));

			return constantExpression;
		}

		protected override Expression VisitConstantPlaceholder(SqlConstantPlaceholderExpression constantPlaceholder)
		{
			output.AppendFormat("$${0}", constantPlaceholder.Index);

			return constantPlaceholder;
		}

		protected override ElementInit VisitElementInitializer(ElementInit initializer)
		{
			output.Append("(");

			foreach (var element in initializer.Arguments)
			{
				Visit(element);
				output.Append(", ");
			}

			if (initializer.Arguments.Count > 0)
			{
				output.Length -= 2;
			}

			output.Append(")");

			return initializer;
		}

		protected override IEnumerable<ElementInit> VisitElementInitializerList(ReadOnlyCollection<ElementInit> original)
		{
			output.Append("{");

			foreach (var element in original)
			{
				VisitElementInitializer(element);
				output.Append(", ");
			}

			if (original.Count > 0)
			{
				output.Length -= 2;
			}

			output.Append("}");

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
			output.AppendFormat("{0}.{1}", methodCallExpression.Method.ReflectedType.FullName, methodCallExpression.Method.Name);
			output.Append("(");
			foreach (var arg in methodCallExpression.Arguments)
			{
				Visit(arg);
				output.Append(", ");
			}

			if (methodCallExpression.Arguments.Count > 0)
			{
				output.Length -= 2;
			}

			output.Append(")");

			return methodCallExpression;
		}

		protected override IEnumerable<MemberBinding> VisitBindingList(ReadOnlyCollection<MemberBinding> original)
		{
			output.Append("{");

			foreach (var binding in original)
			{
				VisitBinding(binding);
				output.Append(", ");
			}

			if (original.Count > 0)
			{
				output.Length -= 2;
			}

			output.Append("}");

			return original;
		}

		protected override Expression VisitMemberInit(MemberInitExpression expression)
		{
			VisitNew(expression.NewExpression);
			VisitBindingList(expression.Bindings);

			return expression;
		}

		protected override Expression VisitNew(NewExpression expression)
		{
			output.AppendFormat("new {0}", expression.Constructor.ReflectedType.FullName);
			output.Append("(");

			foreach (var arg in expression.Arguments)
			{
				Visit(arg);
				output.Append(", ");
			}

			if (expression.Arguments.Count > 0)
			{
				output.Length -= 2;
			}

			output.Append(")");

			return base.VisitNew(expression);
		}
	}
}
