// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq.Expressions;
using Platform.Collections;

namespace Shaolinq.Persistence.Linq.Expressions
{
	[Flags]
	public enum SqlExpressionComparerOptions
	{
		None,
		IgnoreConstants = 1,
		IgnoreConstantPlaceholders = 2,
		IgnoreConstantsAndConstantPlaceholders = IgnoreConstants | IgnoreConstantPlaceholders
	}

    public class SqlExpressionComparer
		: SqlExpressionVisitor
	{
		private bool result;
		private object currentObject;
		private SqlExpressionComparerOptions options;
		
		public SqlExpressionComparer(Expression toCompareTo)
		{
			this.result = true;
			this.currentObject = toCompareTo;
		}

		public static bool Equals(Expression left, Expression right)
		{
			return Equals(left, right, SqlExpressionComparerOptions.None);
		}

		public static bool Equals(Expression left, Expression right, SqlExpressionComparerOptions options)
		{
			var visitor = new SqlExpressionComparer(right) { options = options };
			
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
			if (paramValue == null && this.currentObject == null)
			{
				current = null;

				return false;
			}

			current = this.currentObject as T;

			if (paramValue == null)
			{
				this.result = false;
				
				return false;
			}

			if (current == null)
			{
				this.result = false;

				return false;
			}

			return true;
		}

		protected override MemberBinding VisitBinding(MemberBinding binding)
		{
			MemberBinding current;

			if (!this.TryGetCurrent(binding, out current))
			{
				return binding;
			}

			if (current.BindingType != binding.BindingType)
			{
				this.result = false;

				return binding;
			}

			if (current.Member != binding.Member)
			{
				this.result = false;

				return binding;
			}

			return binding;
		}

		protected override ElementInit VisitElementInitializer(ElementInit initializer)
		{
			ElementInit current;

			if (!this.TryGetCurrent(initializer, out current))
			{
				return initializer;
			}
			
			if (current.Arguments.Count != initializer.Arguments.Count)
			{
				this.result = false;

				return initializer;
			}

			for (var i = 0; i < current.Arguments.Count; i++)
			{
				this.currentObject = current.Arguments[i];
				this.Visit(initializer.Arguments[i]);
			}

			this.currentObject = current;

			return initializer;
		}

		protected override Expression VisitUnary(UnaryExpression unaryExpression)
		{
			UnaryExpression current;

			if (!this.TryGetCurrent(unaryExpression, out current))
			{
				return unaryExpression;
			}

			this.result = this.result && (current.IsLifted == unaryExpression.IsLifted
			                    && current.IsLiftedToNull == unaryExpression.IsLiftedToNull
			                    && current.Method == unaryExpression.Method);

			if (this.result)
			{
				this.currentObject = current.Operand;

				this.Visit(unaryExpression.Operand);
			}

			return unaryExpression;
		}

		protected override Expression VisitBinary(BinaryExpression binaryExpression)
		{
			BinaryExpression current;

			if (!this.TryGetCurrent(binaryExpression, out current))
			{
				return binaryExpression;
			}

			this.currentObject = current.Left;
			this.Visit(binaryExpression.Left);
			this.currentObject = current.Right;
			this.Visit(binaryExpression.Right);
			this.currentObject = current;

			return binaryExpression;
		}

		protected override Expression VisitTypeIs(TypeBinaryExpression expression)
		{
			TypeBinaryExpression current;

			if (!this.TryGetCurrent(expression, out current))
			{
				return expression;
			}

			this.result = this.result && (current.TypeOperand == expression.TypeOperand);

			if (this.result)
			{
				this.currentObject = current.Expression;

				this.Visit(expression.Expression);

				this.currentObject = current;
			}

			return expression;
		}

		protected override Expression VisitConstant(ConstantExpression constantExpression)
		{
			ConstantExpression current;

			if (!this.TryGetCurrent(constantExpression, out current))
			{
				return constantExpression;
			}

			if ((this.options & SqlExpressionComparerOptions.IgnoreConstantPlaceholders) != 0)
			{
				this.result = constantExpression.Type != current.Type;

				return constantExpression;
			}

			if (!Object.Equals(current.Value, constantExpression.Value))
			{
				this.result = false;
			}

			return constantExpression;
		}

		protected override Expression VisitConditional(ConditionalExpression expression)
		{
			ConditionalExpression current;

			if (!this.TryGetCurrent(expression, out current))
			{
				return expression;
			}

			if (this.result)
			{
				this.currentObject = current.Test;
				this.Visit(expression.Test);
			}

			if (this.result)
			{
				this.currentObject = current.IfTrue;
				this.Visit(expression.IfTrue);
			}

			if (this.result)
			{
				this.currentObject = current.IfFalse;
				this.Visit(expression.IfFalse);
			}

			this.currentObject = expression;

			return expression;
		}

		protected override Expression VisitParameter(ParameterExpression expression)
		{
			ParameterExpression current;

			if (!this.TryGetCurrent(expression, out current))
			{
				return expression;
			}

			this.result = this.result && (current.Name == expression.Name);

			return expression;
		}

		protected override Expression VisitMemberAccess(MemberExpression memberExpression)
		{
			MemberExpression current;

			if (!this.TryGetCurrent(memberExpression, out current))
			{
				return memberExpression;
			}

			this.result = this.result && (current.Member == memberExpression.Member);

			if (this.result)
			{
				this.currentObject = current.Expression;
				this.Visit(memberExpression.Expression);
				this.currentObject = current;
			}

			return memberExpression;
		}

		protected override Expression VisitMethodCall(MethodCallExpression methodCallExpression)
		{
			MethodCallExpression current;

			if (!this.TryGetCurrent(methodCallExpression, out current))
			{
				return methodCallExpression;
			}

			this.result = this.result && (current.Method == methodCallExpression.Method);

			if (this.result)
			{
				this.currentObject = current.Object;
				this.Visit(methodCallExpression.Object);
				this.currentObject = current;
			}

			if (this.result)
			{
				this.currentObject = current.Arguments;
				this.VisitExpressionList(methodCallExpression.Arguments);
				this.currentObject = current;
			}

			return methodCallExpression;
		}

		protected override IReadOnlyList<T> VisitExpressionList<T>(IReadOnlyList<T> original)
		{
			IReadOnlyList<Expression> current;

			if (!this.TryGetCurrent(original, out current))
			{
				return original;
			}

			for (var i = 0; i < original.Count; i++)
			{
				this.currentObject = current[i];

				this.Visit(original[i]);

				if (!this.result)
				{
					break;
				}
			}

			this.currentObject = current;

			return original;
		}

		protected override ReadOnlyCollection<Expression> VisitExpressionList(ReadOnlyCollection<Expression> original)
		{
			ReadOnlyCollection<Expression> current;

			if (!this.TryGetCurrent(original, out current))
			{
				return original;
			}

			for (var i = 0; i < original.Count; i++)
			{
				this.currentObject = current[i];

				this.Visit(original[i]);

				if (!this.result)
				{
					break;
				}
			}

			this.currentObject = current;

			return original;
		}

		protected override MemberAssignment VisitMemberAssignment(MemberAssignment assignment)
		{
			MemberAssignment current;

			if (!this.TryGetCurrent(assignment, out current))
			{
				return assignment;
			}

			this.result = this.result && (current.BindingType == assignment.BindingType
			                    && current.Member == assignment.Member);

			if (this.result)
			{
				this.currentObject = current.Expression;
				this.Visit(assignment.Expression);
				this.currentObject = current;
			}

			return assignment;
		}

		protected override MemberMemberBinding VisitMemberMemberBinding(MemberMemberBinding binding)
		{
			MemberMemberBinding current;

			if (!this.TryGetCurrent(binding, out current))
			{
				return binding;
			}

			this.result = this.result && (current.BindingType == binding.BindingType
			                    && current.Member == binding.Member);

			if (this.result)
			{
				this.currentObject = current.Bindings;
				this.VisitBindingList(binding.Bindings);
				this.currentObject = current;
			}

			return binding;
		}

		protected override MemberListBinding VisitMemberListBinding(MemberListBinding binding)
		{
			MemberListBinding current;

			if (!this.TryGetCurrent(binding, out current))
			{
				return binding;
			}

			this.result = this.result && (current.BindingType == binding.BindingType
			                    && current.Member == binding.Member);

			if (this.result)
			{
				this.currentObject = current.Initializers;
				this.VisitElementInitializerList(binding.Initializers);
				this.currentObject = current;
			}

			return binding;
		}

		protected override IEnumerable<MemberBinding> VisitBindingList(ReadOnlyCollection<MemberBinding> original)
		{
			ReadOnlyCollection<MemberBinding> current;

			if (!this.TryGetCurrent(original, out current))
			{
				return original;
			}

			this.result = this.result && (current.Count == original.Count);

			if (this.result)
			{
				for (var i = 0; i < original.Count; i++)
				{
					this.currentObject = current[i];
					this.VisitBinding(original[i]);
				}

				this.currentObject = current;
			}

			return original;
		}

		protected override IEnumerable<ElementInit> VisitElementInitializerList(ReadOnlyCollection<ElementInit> original)
		{
			ReadOnlyCollection<ElementInit> current;

			if (!this.TryGetCurrent(original, out current))
			{
				return original;
			}

			this.result = this.result && (current.Count == original.Count);

			if (this.result)
			{
				for (var i = 0; i < original.Count; i++)
				{
					this.currentObject = current[i];
					this.VisitElementInitializer(original[i]);
				}

				this.currentObject = current;
			}

			return original;
		}

		protected override Expression VisitLambda(LambdaExpression expression)
		{
			LambdaExpression current;

			if (!this.TryGetCurrent(expression, out current))
			{
				return expression;
			}

			this.result = this.result && (current.Parameters.Count == expression.Parameters.Count);

			if (this.result)
			{
				this.currentObject = current.Body;
				this.Visit(expression.Body);
			}

			if (this.result)
			{
				for (var i = 0; i < current.Parameters.Count; i++)
				{
					this.currentObject = current.Parameters[i];

					this.Visit(expression.Parameters[i]);
				}

				this.currentObject = current;
			}

			return expression;
		}

		protected override Expression VisitNew(NewExpression expression)
		{
			NewExpression current;

			if (!this.TryGetCurrent(expression, out current))
			{
				return expression;
			}

			this.result = this.result && (current.Constructor == expression.Constructor
			                    && current.Arguments.Count == expression.Arguments.Count);

			if (this.result)
			{
				for (var i = 0; i < current.Arguments.Count; i++)
				{
					this.currentObject = current.Arguments[i];

					this.Visit(expression.Arguments[i]);
				}

				this.currentObject = current;
			}

			return expression;
		}

		protected override Expression VisitMemberInit(MemberInitExpression expression)
		{
			MemberInitExpression current;

			if (!this.TryGetCurrent(expression, out current))
			{
				return expression;
			}

			this.result = this.result && (current.Bindings.Count == expression.Bindings.Count);

			if (this.result)
			{
				this.currentObject = current.NewExpression;

				this.Visit(expression.NewExpression);

				if (this.result)
				{
					this.currentObject = current.Bindings;
					this.VisitBindingList(expression.Bindings);
				}

				this.currentObject = current;
			}

			return expression;
		}

		protected override Expression VisitListInit(ListInitExpression expression)
		{
			ListInitExpression current;

			if (!this.TryGetCurrent(expression, out current))
			{
				return expression;
			}

			this.result = this.result && (current.Initializers.Count == expression.Initializers.Count);

			if (this.result)
			{
				this.currentObject = current.Initializers;
				this.VisitElementInitializerList(expression.Initializers);
				this.currentObject = current;
			}

			return expression;
		}

		protected override Expression VisitNewArray(NewArrayExpression expression)
		{
			NewArrayExpression current;

			if (!this.TryGetCurrent(expression, out current))
			{
				return expression;
			}

			this.result = this.result && (current.Expressions.Count == expression.Expressions.Count);

			if (this.result)
			{
				this.currentObject = current.Expressions;
				this.VisitExpressionList(expression.Expressions);
				this.currentObject = current;
			}

			return expression;
		}

		protected override Expression VisitInvocation(InvocationExpression expression)
		{
			InvocationExpression current;

			if (!this.TryGetCurrent(expression, out current))
			{
				return expression;
			}

			this.result = this.result && (current.Arguments.Count == expression.Arguments.Count);

			if (this.result)
			{
				this.currentObject = current.Expression;
				this.Visit(expression.Expression);
			}

			return expression;
		}

		protected override Expression VisitConstantPlaceholder(SqlConstantPlaceholderExpression constantPlaceholder)
		{
			SqlConstantPlaceholderExpression current;

			if (!this.TryGetCurrent(constantPlaceholder, out current))
			{
				return constantPlaceholder;
			}

			this.result = this.result && (current.Index == constantPlaceholder.Index);

			if (!this.result)
			{
				return constantPlaceholder;
			}

			if ((this.options & SqlExpressionComparerOptions.IgnoreConstantPlaceholders) != 0)
			{
				this.result = current.Type == constantPlaceholder.Type;

				return constantPlaceholder;
			}

			this.currentObject = current.ConstantExpression;
			this.Visit(constantPlaceholder.ConstantExpression);
			this.currentObject = current;

			return constantPlaceholder;
		}

		protected override Expression VisitObjectReference(SqlObjectReferenceExpression objectReference)
		{
			SqlObjectReferenceExpression current;

			if (!this.TryGetCurrent(objectReference, out current))
			{
				return objectReference;
			}

			if (Object.Equals(current.Type, objectReference.Type))
			{
				return objectReference;
			}

			this.result = this.result && (current.Bindings.Count == objectReference.Bindings.Count);

			if (this.result)
			{
				this.currentObject = current.Bindings;
				this.VisitBindingList(objectReference.Bindings);
				this.currentObject = current;
			}
			
			return objectReference;
		}

		protected override Expression VisitJoin(SqlJoinExpression join)
		{
			SqlJoinExpression current;

			if (!this.TryGetCurrent(join, out current))
			{
				return join;
			}

			this.result = this.result && (current.JoinType == join.JoinType);

			if (this.result)
			{
				this.currentObject = current.JoinCondition;
				this.Visit(join.JoinCondition);

				this.currentObject = current.Left;
				this.Visit(join.Left);

				this.currentObject = current.Right;
				this.Visit(join.Right);

				this.currentObject = current;
			}

			return join;
		}

		protected override Expression VisitTable(SqlTableExpression table)
		{
			SqlTableExpression current;

			if (!this.TryGetCurrent(table, out current))
			{
				return table;
			}

			this.result = this.result && (current.Name == table.Name && current.Alias == table.Alias);

			return table;
		}

		protected override Expression VisitColumn(SqlColumnExpression columnExpression)
		{
			SqlColumnExpression current;

			if (!this.TryGetCurrent(columnExpression, out current))
			{
				return columnExpression;
			}

			this.result = this.result && (current.Name == columnExpression.Name
			                    && current.SelectAlias == columnExpression.SelectAlias);

			return columnExpression;
		}

		protected override Expression VisitFunctionCall(SqlFunctionCallExpression functionCallExpression)
		{
			SqlFunctionCallExpression current;

			if (!this.TryGetCurrent(functionCallExpression, out current))
			{
				return functionCallExpression;
			}

			this.result = this.result && (current.Function == functionCallExpression.Function);

			if (this.result)
			{
				this.currentObject = current.Arguments;
				this.VisitExpressionList(functionCallExpression.Arguments);
				this.currentObject = current;
			}

			return functionCallExpression;
		}

		protected override Expression VisitSubquery(SqlSubqueryExpression subquery)
		{
			SqlSubqueryExpression current;

			if (!this.TryGetCurrent(subquery, out current))
			{
				return subquery;
			}

			this.currentObject = current.Select;
			this.VisitSelect(subquery.Select);
			this.currentObject = current;
			
			return subquery;
		}

		protected override Expression VisitAggregate(SqlAggregateExpression sqlAggregate)
		{
			SqlAggregateExpression current;

			if (!this.TryGetCurrent(sqlAggregate, out current))
			{
				return sqlAggregate;
			}

			this.result &= current.IsDistinct == sqlAggregate.IsDistinct
			          && current.AggregateType == sqlAggregate.AggregateType;

			if (this.result)
			{
				this.currentObject = current.Argument;
				this.Visit(sqlAggregate.Argument);
				this.currentObject = current;
			}

			return sqlAggregate;
		}

		protected override Expression VisitAggregateSubquery(SqlAggregateSubqueryExpression aggregate)
		{
			SqlAggregateSubqueryExpression current;

			if (!this.TryGetCurrent(aggregate, out current))
			{
				return aggregate;
			}

			this.result = this.result && (current.GroupByAlias == aggregate.GroupByAlias);

			if (this.result)
			{
				this.currentObject = current.AggregateAsSubquery;
				this.Visit(aggregate.AggregateAsSubquery);

				if (this.result)
				{
					this.currentObject = current.AggregateInGroupSelect;
					this.Visit(aggregate.AggregateInGroupSelect);
				}
			}

			this.currentObject = current;

			return aggregate;
		}

		protected override IReadOnlyList<SqlColumnDeclaration> VisitColumnDeclarations(IReadOnlyList<SqlColumnDeclaration> columns)
		{
			IReadOnlyList<SqlColumnDeclaration> current;

			if (!this.TryGetCurrent(columns, out current))
			{
				return columns;
			}

			this.result = this.result && (current.Count == columns.Count);

			if (this.result)
			{
				for (var i = 0; i < current.Count; i++)
				{
					if (current[i].Name != columns[i].Name)
					{
						this.result = false;

						break;
					}

					this.currentObject = current[i].Expression;
					this.Visit(columns[i].Expression);

					if (!this.result)
					{
						break;
					}
				}

				this.currentObject = current;
			}

			return columns;
		}

		protected override Expression VisitOrderBy(SqlOrderByExpression orderByExpression)
		{
			SqlOrderByExpression current;

			if (!this.TryGetCurrent(orderByExpression, out current))
			{
				return orderByExpression;
			}

			this.result = this.result && (current.OrderType == orderByExpression.OrderType);

			if (this.result)
			{
				this.currentObject = current.Expression;
				this.Visit(orderByExpression.Expression);
				this.currentObject = current;
			}

			return orderByExpression;
		}

		protected override Expression VisitSelect(SqlSelectExpression selectExpression)
		{
			SqlSelectExpression current;

			if (!this.TryGetCurrent(selectExpression, out current))
			{
				return selectExpression;
			}

			this.result = this.result && (current.Alias == selectExpression.Alias
			                    && current.Distinct == selectExpression.Distinct
			                    && current.ForUpdate == selectExpression.ForUpdate);

			if (this.result)
			{
				this.currentObject = current.Skip;

				this.Visit(current.Skip);
			}

			if (this.result)
			{
				this.currentObject = current.Take;

				this.Visit(current.Take);
			}

			if (!this.result)
			{
				return selectExpression;
			}

			if (this.result)
			{
				this.currentObject = current.Columns;
				this.VisitColumnDeclarations(selectExpression.Columns);
				this.currentObject = current;
			}

			if (this.result)
			{
				this.currentObject = current.Where;
				this.Visit(selectExpression.Where);
				this.currentObject = current;
			}

			if (this.result)
			{
				this.currentObject = current.GroupBy;
				this.VisitExpressionList(selectExpression.GroupBy);
				this.currentObject = current;
			}

			if (this.result)
			{
				this.currentObject = current.OrderBy;
				this.VisitExpressionList(selectExpression.OrderBy);
				this.currentObject = current;
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

			if (!this.TryGetCurrent(projection, out current))
			{
				return projection;
			}

			this.result = this.result && (current.IsDefaultIfEmpty == projection.IsDefaultIfEmpty
			                    && current.IsElementTableProjection == projection.IsElementTableProjection
			                    && current.SelectFirstType == projection.SelectFirstType);
					  
			if (!this.result)
			{
				return projection;
			}

			this.currentObject = current.Aggregator;
			this.Visit(projection.Aggregator);

			if (!this.result)
			{
				return projection;
			}

			this.currentObject = current.DefaultValueExpression;
			this.Visit(projection.DefaultValueExpression);

			if (!this.result)
			{
				return projection;
			}

			this.currentObject = current.Projector;
			this.Visit(projection.Projector);

			if (!this.result)
			{
				return projection;
			}

			this.currentObject = current.Select;
			this.Visit(projection.Select);

			if (!this.result)
			{
				return projection;
			}

			return projection;
		}

		protected override Expression VisitDelete(SqlDeleteExpression deleteExpression)
		{
			SqlDeleteExpression current;

			if (!this.TryGetCurrent(deleteExpression, out current))
			{
				return deleteExpression;
			}

			this.result = this.result && (current.Alias == deleteExpression.Alias
			                    && current.Table == deleteExpression.Table);

			if (this.result)
			{
				this.currentObject = current.Where;
				this.Visit(deleteExpression.Where);
				this.currentObject = current;
			}

			return deleteExpression;
		}

		protected override Expression VisitOver(SqlOverExpression expression)
		{
			// TODO
			return base.VisitOver(expression);
		}

		protected override Expression VisitSetCommand(SqlSetCommandExpression expression)
		{
			SqlSetCommandExpression current;

			if (!this.TryGetCurrent(expression, out current))
			{
				return expression;
			}

			if (expression.ConfigurationParameter != current.ConfigurationParameter)
			{
				this.result = false;

				return expression;
			}

			this.currentObject = current.Target;
			this.Visit(expression.Target);
			this.currentObject = current;

			if (!this.result)
			{
				return expression;
			}

			this.VisitExpressionList(expression.Arguments);

			if (!this.result)
			{
				return expression;
			}

			return expression;
		}
	}
}

