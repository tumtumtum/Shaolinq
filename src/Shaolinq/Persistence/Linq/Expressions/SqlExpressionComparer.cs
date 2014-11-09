// Copyright (c) 2007-2014 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Expressions
{
	public class SqlExpressionComparer
		: SqlExpressionVisitor
	{
		private bool result;
		private object currentObject;
		private bool ignoreConstantPlaceholderValues;
		
		public SqlExpressionComparer(Expression toCompareTo)
		{
			this.result = true;
			currentObject = toCompareTo;
		}

		public static bool Equals(Expression left, Expression right)
		{
			return Equals(left, right, false);
		}

		public static bool Equals(Expression left, Expression right, bool ignoreConstantPlaceholderValues)
		{
			var visitor = new SqlExpressionComparer(right);

			visitor.ignoreConstantPlaceholderValues = ignoreConstantPlaceholderValues;
			
			visitor.Visit(left);

			return visitor.result;
		}

		protected override Expression Visit(Expression expression)
		{
			return base.Visit(expression);
		}

		private bool TryGetCurrent<T>(T paramValue, out T current)
			where T : class
		{
			if (paramValue == null && currentObject == null)
			{
				current = null;

				return false;
			}

			current = currentObject as T;

			if (paramValue == null)
			{
				result = false;
				
				return false;
			}

			if (current == null)
			{
				result = false;

				return false;
			}

			return true;
		}

		protected override MemberBinding VisitBinding(MemberBinding binding)
		{
			MemberBinding current;

			if (!TryGetCurrent(binding, out current))
			{
				return binding;
			}

			if (current.BindingType != binding.BindingType)
			{
				result = false;

				return binding;
			}

			if (current.Member != binding.Member)
			{
				result = false;

				return binding;
			}

			return binding;
		}

		protected override ElementInit VisitElementInitializer(ElementInit initializer)
		{
			ElementInit current;

			if (!TryGetCurrent(initializer, out current))
			{
				return initializer;
			}
			
			if (current.Arguments.Count != initializer.Arguments.Count)
			{
				result = false;

				return initializer;
			}

			for (var i = 0; i < current.Arguments.Count; i++)
			{
				currentObject = current.Arguments[i];
				Visit(initializer.Arguments[i]);
			}

			currentObject = current;

			return initializer;
		}

		protected override Expression VisitUnary(UnaryExpression unaryExpression)
		{
			UnaryExpression current;

			if (!TryGetCurrent(unaryExpression, out current))
			{
				return unaryExpression;
			}

			result = result && (current.IsLifted == unaryExpression.IsLifted
			                    && current.IsLiftedToNull == unaryExpression.IsLiftedToNull
			                    && current.Method == unaryExpression.Method);

			if (result)
			{
				currentObject = current.Operand;

				Visit(unaryExpression.Operand);
			}

			return unaryExpression;
		}

		protected override Expression VisitBinary(BinaryExpression binaryExpression)
		{
			BinaryExpression current;

			if (!TryGetCurrent(binaryExpression, out current))
			{
				return binaryExpression;
			}

			currentObject = current.Left;
			Visit(binaryExpression.Left);
			currentObject = current.Right;
			Visit(binaryExpression.Right);
			currentObject = current;

			return binaryExpression;
		}

		protected override Expression VisitTypeIs(TypeBinaryExpression expression)
		{
			TypeBinaryExpression current;

			if (!TryGetCurrent(expression, out current))
			{
				return expression;
			}

			result = result && (current.TypeOperand == expression.TypeOperand);

			if (result)
			{
				currentObject = current.Expression;

				Visit(expression.Expression);

				currentObject = current;
			}

			return expression;
		}

		protected override Expression VisitConstant(ConstantExpression constantExpression)
		{
			ConstantExpression current;

			if (!TryGetCurrent(constantExpression, out current))
			{
				return constantExpression;
			}

			if (!Object.Equals(current.Value, constantExpression.Value))
			{
				result = false;
			}

			return constantExpression;
		}

		protected override Expression VisitConditional(ConditionalExpression expression)
		{
			ConditionalExpression current;

			if (!TryGetCurrent(expression, out current))
			{
				return expression;
			}

			if (result)
			{
				currentObject = current.Test;
				Visit(expression.Test);
			}

			if (result)
			{
				currentObject = current.IfTrue;
				Visit(expression.IfTrue);
			}

			if (result)
			{
				currentObject = current.IfFalse;
				Visit(expression.IfFalse);
			}

			currentObject = expression;

			return expression;
		}

		protected override Expression VisitParameter(ParameterExpression expression)
		{
			ParameterExpression current;

			if (!TryGetCurrent(expression, out current))
			{
				return expression;
			}

			result = result && (current.Name == expression.Name);

			return expression;
		}

		protected override Expression VisitMemberAccess(MemberExpression memberExpression)
		{
			MemberExpression current;

			if (!TryGetCurrent(memberExpression, out current))
			{
				return memberExpression;
			}

			result = result && (current.Member == memberExpression.Member);

			if (result)
			{
				currentObject = current.Expression;
				Visit(memberExpression.Expression);
				currentObject = current;
			}

			return memberExpression;
		}

		protected override Expression VisitMethodCall(MethodCallExpression methodCallExpression)
		{
			MethodCallExpression current;

			if (!TryGetCurrent(methodCallExpression, out current))
			{
				return methodCallExpression;
			}

			result = result && (current.Method == methodCallExpression.Method);

			if (result)
			{
				currentObject = current.Object;
				Visit(methodCallExpression.Object);
				currentObject = current;
			}

			if (result)
			{
				currentObject = current.Arguments;
				VisitExpressionList(methodCallExpression.Arguments);
				currentObject = current;
			}

			return methodCallExpression;
		}

		protected override ReadOnlyCollection<Expression> VisitExpressionList(ReadOnlyCollection<Expression> original)
		{
			ReadOnlyCollection<Expression> current;

			if (!TryGetCurrent(original, out current))
			{
				return original;
			}

			for (var i = 0; i < original.Count; i++)
			{
				currentObject = current[i];

				Visit(original[i]);

				if (!result)
				{
					break;
				}
			}

			currentObject = current;

			return original;
		}

		protected override MemberAssignment VisitMemberAssignment(MemberAssignment assignment)
		{
			MemberAssignment current;

			if (!TryGetCurrent(assignment, out current))
			{
				return assignment;
			}

			result = result && (current.BindingType == assignment.BindingType
			                    && current.Member == assignment.Member);

			if (result)
			{
				currentObject = current.Expression;
				Visit(assignment.Expression);
				currentObject = current;
			}

			return assignment;
		}

		protected override MemberMemberBinding VisitMemberMemberBinding(MemberMemberBinding binding)
		{
			MemberMemberBinding current;

			if (!TryGetCurrent(binding, out current))
			{
				return binding;
			}

			result = result && (current.BindingType == binding.BindingType
			                    && current.Member == binding.Member);

			if (result)
			{
				currentObject = current.Bindings;
				VisitBindingList(binding.Bindings);
				currentObject = current;
			}

			return binding;
		}

		protected override MemberListBinding VisitMemberListBinding(MemberListBinding binding)
		{
			MemberListBinding current;

			if (!TryGetCurrent(binding, out current))
			{
				return binding;
			}

			result = result && (current.BindingType == binding.BindingType
			                    && current.Member == binding.Member);

			if (result)
			{
				currentObject = current.Initializers;
				VisitElementInitializerList(binding.Initializers);
				currentObject = current;
			}

			return binding;
		}

		protected override IEnumerable<MemberBinding> VisitBindingList(ReadOnlyCollection<MemberBinding> original)
		{
			ReadOnlyCollection<MemberBinding> current;

			if (!TryGetCurrent(original, out current))
			{
				return original;
			}

			result = result && (current.Count == original.Count);

			if (result)
			{
				for (var i = 0; i < original.Count; i++)
				{
					currentObject = current[i];
					VisitBinding(original[i]);
				}

				currentObject = current;
			}

			return original;
		}

		protected override IEnumerable<ElementInit> VisitElementInitializerList(ReadOnlyCollection<ElementInit> original)
		{
			ReadOnlyCollection<ElementInit> current;

			if (!TryGetCurrent(original, out current))
			{
				return original;
			}

			result = result && (current.Count == original.Count);

			if (result)
			{
				for (var i = 0; i < original.Count; i++)
				{
					currentObject = current[i];
					VisitElementInitializer(original[i]);
				}

				currentObject = current;
			}

			return original;
		}

		protected override Expression VisitLambda(LambdaExpression expression)
		{
			LambdaExpression current;

			if (!TryGetCurrent(expression, out current))
			{
				return expression;
			}

			result = result && (current.Parameters.Count == expression.Parameters.Count);

			if (result)
			{
				currentObject = current.Body;
				Visit(expression.Body);
			}

			if (result)
			{
				for (var i = 0; i < current.Parameters.Count; i++)
				{
					currentObject = current.Parameters[i];

					Visit(expression.Parameters[i]);
				}

				currentObject = current;
			}

			return expression;
		}

		protected override NewExpression VisitNew(NewExpression expression)
		{
			NewExpression current;

			if (!TryGetCurrent(expression, out current))
			{
				return expression;
			}

			result = result && (current.Constructor == expression.Constructor
			                    && current.Arguments.Count == expression.Arguments.Count);

			if (result)
			{
				for (var i = 0; i < current.Arguments.Count; i++)
				{
					currentObject = current.Arguments[i];

					Visit(expression.Arguments[i]);
				}

				currentObject = current;
			}

			return expression;
		}

		protected override Expression VisitMemberInit(MemberInitExpression expression)
		{
			MemberInitExpression current;

			if (!TryGetCurrent(expression, out current))
			{
				return expression;
			}

			result = result && (current.Bindings.Count == expression.Bindings.Count);

			if (result)
			{
				currentObject = current.NewExpression;

				Visit(expression.NewExpression);

				if (result)
				{
					currentObject = current.Bindings;
					VisitBindingList(expression.Bindings);
				}

				currentObject = current;
			}

			return expression;
		}

		protected override Expression VisitListInit(ListInitExpression expression)
		{
			ListInitExpression current;

			if (!TryGetCurrent(expression, out current))
			{
				return expression;
			}

			result = result && (current.Initializers.Count == expression.Initializers.Count);

			if (result)
			{
				currentObject = current.Initializers;
				VisitElementInitializerList(expression.Initializers);
				currentObject = current;
			}

			return expression;
		}

		protected override Expression VisitNewArray(NewArrayExpression expression)
		{
			NewArrayExpression current;

			if (!TryGetCurrent(expression, out current))
			{
				return expression;
			}

			result = result && (current.Expressions.Count == expression.Expressions.Count);

			if (result)
			{
				currentObject = current.Expressions;
				VisitExpressionList(expression.Expressions);
				currentObject = current;
			}

			return expression;
		}

		protected override Expression VisitInvocation(InvocationExpression expression)
		{
			InvocationExpression current;

			if (!TryGetCurrent(expression, out current))
			{
				return expression;
			}

			result = result && (current.Arguments.Count == expression.Arguments.Count);

			if (result)
			{
				currentObject = current.Expression;
				Visit(expression.Expression);
			}

			return expression;
		}

		protected override Expression VisitConstantPlaceholder(SqlConstantPlaceholderExpression constantPlaceholder)
		{
			SqlConstantPlaceholderExpression current;

			if (!TryGetCurrent(constantPlaceholder, out current))
			{
				return constantPlaceholder;
			}

			result = result && (current.Index == constantPlaceholder.Index);

			if (result && !ignoreConstantPlaceholderValues)
			{
				currentObject = current.ConstantExpression;
				Visit(constantPlaceholder.ConstantExpression);
				currentObject = current;
			}

			return constantPlaceholder;
		}

		protected override Expression VisitObjectReference(SqlObjectReferenceExpression objectReference)
		{
			SqlObjectReferenceExpression current;

			if (!TryGetCurrent(objectReference, out current))
			{
				return objectReference;
			}

			if (Object.Equals(current.Type, objectReference.Type))
			{
				return objectReference;
			}

			result = result && (current.Bindings.Count == objectReference.Bindings.Count);

			if (result)
			{
				currentObject = current.Bindings;
				VisitBindingList(objectReference.Bindings);
				currentObject = current;
			}
			
			return objectReference;
		}

		protected override Expression VisitJoin(SqlJoinExpression join)
		{
			SqlJoinExpression current;

			if (!TryGetCurrent(join, out current))
			{
				return join;
			}

			result = result && (current.JoinType == join.JoinType);

			if (result)
			{
				currentObject = current.Condition;
				Visit(join.Condition);

				currentObject = current.Left;
				Visit(join.Left);

				currentObject = current.Right;
				Visit(join.Right);

				currentObject = current;
			}

			return join;
		}

		protected override Expression VisitTable(SqlTableExpression table)
		{
			SqlTableExpression current;

			if (!TryGetCurrent(table, out current))
			{
				return table;
			}

			result = result && (current.Name == table.Name && current.Alias == table.Alias);

			return table;
		}

		protected override Expression VisitColumn(SqlColumnExpression columnExpression)
		{
			SqlColumnExpression current;

			if (!TryGetCurrent(columnExpression, out current))
			{
				return columnExpression;
			}

			result = result && (current.Name == columnExpression.Name
			                    && current.SelectAlias == columnExpression.SelectAlias);

			return columnExpression;
		}

		protected override Expression VisitFunctionCall(SqlFunctionCallExpression functionCallExpression)
		{
			SqlFunctionCallExpression current;

			if (!TryGetCurrent(functionCallExpression, out current))
			{
				return functionCallExpression;
			}

			result = result && (current.Function == functionCallExpression.Function);

			if (result)
			{
				currentObject = current.Arguments;
				VisitExpressionList(functionCallExpression.Arguments);
				currentObject = current;
			}

			return functionCallExpression;
		}

		protected override Expression VisitSubquery(SqlSubqueryExpression subquery)
		{
			SqlSubqueryExpression current;

			if (!TryGetCurrent(subquery, out current))
			{
				return subquery;
			}

			currentObject = current.Select;
			VisitSelect(subquery.Select);
			currentObject = current;
			
			return subquery;
		}

		protected override Expression VisitAggregate(SqlAggregateExpression sqlAggregate)
		{
			SqlAggregateExpression current;

			if (!TryGetCurrent(sqlAggregate, out current))
			{
				return sqlAggregate;
			}

			result &= current.IsDistinct == sqlAggregate.IsDistinct
			          && current.AggregateType == sqlAggregate.AggregateType;

			if (result)
			{
				currentObject = current.Argument;
				Visit(sqlAggregate.Argument);
				currentObject = current;
			}

			return sqlAggregate;
		}

		protected override Expression VisitAggregateSubquery(SqlAggregateSubqueryExpression aggregate)
		{
			SqlAggregateSubqueryExpression current;

			if (!TryGetCurrent(aggregate, out current))
			{
				return aggregate;
			}

			result = result && (current.GroupByAlias == aggregate.GroupByAlias);

			if (result)
			{
				currentObject = current.AggregateAsSubquery;
				Visit(aggregate.AggregateAsSubquery);

				if (result)
				{
					currentObject = current.AggregateInGroupSelect;
					Visit(aggregate.AggregateInGroupSelect);
				}
			}

			currentObject = current;

			return aggregate;
		}

		protected override ReadOnlyCollection<SqlColumnDeclaration> VisitColumnDeclarations(ReadOnlyCollection<SqlColumnDeclaration> columns)
		{
			ReadOnlyCollection<SqlColumnDeclaration> current;

			if (!TryGetCurrent(columns, out current))
			{
				return columns;
			}

			result = result && (current.Count == columns.Count);

			if (result)
			{
				for (var i = 0; i < current.Count; i++)
				{
					if (current[i].Name != columns[i].Name)
					{
						result = false;

						break;
					}

					currentObject = current[i].Expression;
					Visit(columns[i].Expression);

					if (!result)
					{
						break;
					}
				}

				currentObject = current;
			}

			return columns;
		}

		protected override Expression VisitOrderBy(SqlOrderByExpression orderByExpression)
		{
			SqlOrderByExpression current;

			if (!TryGetCurrent(orderByExpression, out current))
			{
				return orderByExpression;
			}

			result = result && (current.OrderType == orderByExpression.OrderType);

			if (result)
			{
				currentObject = current.Expression;
				Visit(orderByExpression.Expression);
				currentObject = current;
			}

			return orderByExpression;
		}

		protected override Expression VisitSelect(SqlSelectExpression selectExpression)
		{
			SqlSelectExpression current;

			if (!TryGetCurrent(selectExpression, out current))
			{
				return selectExpression;
			}

			result = result && (current.Alias == selectExpression.Alias
			                    && current.Distinct == selectExpression.Distinct
			                    && current.ForUpdate == selectExpression.ForUpdate);

			if (result)
			{
				currentObject = current.Skip;

				this.Visit(current.Skip);
			}

			if (result)
			{
				currentObject = current.Take;

				this.Visit(current.Take);
			}

			if (!result)
			{
				return selectExpression;
			}

			if (result)
			{
				currentObject = current.Columns;
				VisitColumnDeclarations(selectExpression.Columns);
				currentObject = current;
			}

			if (result)
			{
				currentObject = current.Where;
				Visit(selectExpression.Where);
				currentObject = current;
			}

			if (result)
			{
				currentObject = current.GroupBy;
				VisitExpressionList(selectExpression.GroupBy);
				currentObject = current;
			}

			if (result)
			{
				currentObject = current.OrderBy;
				this.VisitExpressionList(selectExpression.OrderBy);
				currentObject = current;
			}


			return selectExpression;
		}

		protected override Expression VisitSource(Expression source)
		{
			return base.VisitSource(source);
		}

		protected override Expression VisitProjection(SqlProjectionExpression projection)
		{
			SqlProjectionExpression current;

			if (!TryGetCurrent(projection, out current))
			{
				return projection;
			}

			result = result && (current.IsDefaultIfEmpty == projection.IsDefaultIfEmpty
			                    && current.IsElementTableProjection == projection.IsElementTableProjection
			                    && current.SelectFirstType == projection.SelectFirstType);
					  
			if (!result)
			{
				return projection;
			}

			currentObject = current.Aggregator;
			Visit(projection.Aggregator);

			if (!result)
			{
				return projection;
			}

			currentObject = current.DefaultValueExpression;
			Visit(projection.DefaultValueExpression);

			if (!result)
			{
				return projection;
			}

			currentObject = current.Projector;
			Visit(projection.Projector);

			if (!result)
			{
				return projection;
			}

			currentObject = current.Select;
			Visit(projection.Select);

			if (!result)
			{
				return projection;
			}

			return projection;
		}

		protected override Expression VisitDelete(SqlDeleteExpression deleteExpression)
		{
			SqlDeleteExpression current;

			if (!TryGetCurrent(deleteExpression, out current))
			{
				return deleteExpression;
			}

			result = result && (current.Alias == deleteExpression.Alias
			                    && current.TableName == deleteExpression.TableName);

			if (result)
			{
				currentObject = current.Where;
				Visit(deleteExpression.Where);
				currentObject = current;
			}

			return deleteExpression;
		}
	}
}

