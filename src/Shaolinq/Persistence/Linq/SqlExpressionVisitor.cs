// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Shaolinq.Persistence.Linq.Expressions;

namespace Shaolinq.Persistence.Linq
{
	public class SqlExpressionVisitor
		: Platform.Linq.ExpressionVisitor
	{
		protected override Expression Visit(Expression expression)
		{
			if (expression == null)
			{
				return null;
			}

			switch ((SqlExpressionType)expression.NodeType)
			{
			case SqlExpressionType.ConstantPlaceholder:
				return this.VisitConstantPlaceholder((SqlConstantPlaceholderExpression)expression);
			case SqlExpressionType.Table:
				return this.VisitTable((SqlTableExpression)expression);
			case SqlExpressionType.Column:
				return this.VisitColumn((SqlColumnExpression)expression);
			case SqlExpressionType.Select:
				return this.VisitSelect((SqlSelectExpression)expression);
			case SqlExpressionType.Join:
				return this.VisitJoin((SqlJoinExpression)expression);
			case SqlExpressionType.Projection:
				return this.VisitProjection((SqlProjectionExpression)expression);
			case SqlExpressionType.FunctionCall:
				return this.VisitFunctionCall((SqlFunctionCallExpression)expression);
			case SqlExpressionType.Aggregate:
				return this.VisitAggregate((SqlAggregateExpression)expression);
			case SqlExpressionType.Subquery:
				return this.VisitSubquery((SqlSubqueryExpression)expression);
			case SqlExpressionType.AggregateSubquery:
				return this.VisitAggregateSubquery((SqlAggregateSubqueryExpression)expression);
			case SqlExpressionType.ObjectReference:
				return this.VisitObjectReference((SqlObjectReferenceExpression)expression);
			case SqlExpressionType.OrderBy:
				return this.VisitOrderBy((SqlOrderByExpression)expression);
			case SqlExpressionType.Tuple:
				return this.VisitTuple((SqlTupleExpression)expression);
			case SqlExpressionType.Delete:
				return this.VisitDelete((SqlDeleteExpression)expression);
			case SqlExpressionType.AlterTable:
				return this.VisitAlterTable((SqlAlterTableExpression)expression);
			case SqlExpressionType.ConstraintAction:
				return this.VisitConstraintAction((SqlConstraintActionExpression)expression);
			case SqlExpressionType.ColumnDefinition:
				return this.VisitColumnDefinition((SqlColumnDefinitionExpression)expression);
			case SqlExpressionType.CreateIndex:
				return this.VisitCreateIndex((SqlCreateIndexExpression)expression);
			case SqlExpressionType.IndexedColumn:
				return this.VisitIndexedColumn((SqlIndexedColumnExpression)expression);
			case SqlExpressionType.CreateTable:
				return this.VisitCreateTable((SqlCreateTableExpression)expression);
			case SqlExpressionType.ForeignKeyConstraint:
				return this.VisitForeignKeyConstraint((SqlForeignKeyConstraintExpression)expression);
			case SqlExpressionType.ReferencesColumn:
				return this.VisitReferencesColumn((SqlReferencesColumnExpression)expression);
			case SqlExpressionType.SimpleConstraint:
				return this.VisitSimpleConstraint((SqlSimpleConstraintExpression)expression);
			case SqlExpressionType.StatementList:
				return this.VisitStatementList((SqlStatementListExpression)expression);
			case SqlExpressionType.InsertInto:
				return this.VisitInsertInto((SqlInsertIntoExpression)expression);
			case SqlExpressionType.Update:
				return this.VisitUpdate((SqlUpdateExpression)expression);
			case SqlExpressionType.Assign:
				return this.VisitAssign((SqlAssignExpression)expression);
			case SqlExpressionType.CreateType:
				return this.VisitCreateType((SqlCreateTypeExpression)expression);
			case SqlExpressionType.Type:
				return this.VisitType((SqlTypeExpression)expression);
			case SqlExpressionType.EnumDefinition:
				return this.VisitEnumDefinition((SqlEnumDefinitionExpression)expression);
			case SqlExpressionType.Pragma:
				return this.VisitPragma((SqlPragmaExpression)expression);
			case SqlExpressionType.SetCommand:
				return this.VisitSetCommand((SqlSetCommandExpression)expression);
			case SqlExpressionType.Over:
				return this.VisitOver((SqlOverExpression)expression);
			case SqlExpressionType.Scalar:
				return this.VisitScalar((SqlScalarExpression)expression);
			default:
				return base.Visit(expression);
			}
		}

		protected virtual Expression VisitScalar(SqlScalarExpression expression)
		{
			var select = this.Visit(expression.Select);

			if (select != expression.Select)
			{
				return expression.ChangeSelect((SqlSelectExpression)select);
			}

			return expression;
		}

		protected virtual Expression VisitOver(SqlOverExpression expression)
		{
			var source = this.Visit(expression.Source);
			var orderBy = this.VisitExpressionList(expression.OrderBy);

			if (source != expression.Source || orderBy != expression.OrderBy)
			{
				return new SqlOverExpression(source, orderBy);
			}

			return expression;
		}

		protected virtual Expression VisitSetCommand(SqlSetCommandExpression expression)
		{
			var target = this.Visit(expression.Target);
			var arguments = this.VisitExpressionList(expression.Arguments);

			if (target != expression.Target || arguments != expression.Arguments)
			{
				return new SqlSetCommandExpression(expression.ConfigurationParameter, target, arguments);
			}

			return expression;
		}

		protected virtual Expression VisitPragma(SqlPragmaExpression expression)
		{
			return expression;
		}

		protected virtual Expression VisitEnumDefinition(SqlEnumDefinitionExpression expression)
		{
			return expression;
		}

		protected virtual Expression VisitType(SqlTypeExpression expression)
		{
			return expression;
		}

		protected virtual Expression VisitCreateType(SqlCreateTypeExpression expression)
		{
			var newSqlType = this.Visit(expression.SqlType);
			var newAsExpression = this.Visit(expression.AsExpression);

			if (newSqlType != expression.SqlType || newAsExpression != expression.AsExpression)
			{
				return new SqlCreateTypeExpression(newSqlType, newAsExpression, false);
			}

			return expression;
		}

		protected virtual Expression VisitAssign(SqlAssignExpression expression)
		{
			var newTarget = this.Visit(expression.Target);
			var newValue = this.Visit(expression.Value);

			if (newValue != expression.Value)
			{
				return new SqlAssignExpression(newTarget, newValue);
			}

			return expression;
		}

		protected virtual Expression VisitUpdate(SqlUpdateExpression expression)
		{
			var newWhere = this.Visit(expression.Where);
			var newAssignments = this.VisitExpressionList(expression.Assignments);

			if (newWhere != expression.Where || newAssignments != expression.Assignments)
			{
				return new SqlUpdateExpression(expression.Table, newAssignments, newWhere);
			}

			return expression;
		}

		protected virtual Expression VisitInsertInto(SqlInsertIntoExpression expression)
		{
			var table = (SqlTableExpression)this.VisitTable(expression.Table);
			var valueExpressions = this.VisitExpressionList(expression.ValueExpressions);

			if (table != expression.Table || valueExpressions != expression.ValueExpressions)
			{
				return expression.ChangeTableAndValueExpressions(table, valueExpressions);
			}

			return expression;
		}

		protected virtual Expression VisitTuple(SqlTupleExpression tupleExpression)
		{
			var expressions = this.VisitExpressionList(tupleExpression.SubExpressions);

			if (tupleExpression.SubExpressions != expressions)
			{
				return new SqlTupleExpression(expressions, tupleExpression.Type);
			}

			return tupleExpression;
		}

		protected virtual Expression VisitConstantPlaceholder(SqlConstantPlaceholderExpression constantPlaceholder)
		{
			var result = this.VisitConstant(constantPlaceholder.ConstantExpression);

			if (constantPlaceholder.ConstantExpression != result)
			{
				if (!(result is ConstantExpression))
				{
					return result;
				}

				return new SqlConstantPlaceholderExpression(constantPlaceholder.Index, (ConstantExpression)result);
			}

			return constantPlaceholder;
		}

		protected virtual Expression VisitObjectReference(SqlObjectReferenceExpression objectReferenceExpression)
		{
			var newBindings = this.VisitBindingList(objectReferenceExpression.Bindings);

			if (!ReferenceEquals(newBindings, objectReferenceExpression.Bindings))
			{
				return new SqlObjectReferenceExpression(objectReferenceExpression.Type, newBindings);
			}
			else
			{
				return objectReferenceExpression;
			}
		}

		protected virtual Expression VisitJoin(SqlJoinExpression join)
		{
			var left = this.Visit(join.Left);
			var right = this.Visit(join.Right);
			var condition = this.Visit(join.JoinCondition);

			if (left != join.Left || right != join.Right || condition != join.JoinCondition)
			{
				return new SqlJoinExpression(join.Type, join.JoinType, left, right, condition);
			}

			return join;
		}

		protected static SqlProjectionExpression UpdateProjection(SqlProjectionExpression projectionExpression, SqlSelectExpression select, Expression projector, LambdaExpression aggregator)
		{
			if (select != projectionExpression.Select || projector != projectionExpression.Projector || aggregator != projectionExpression.Aggregator)
			{
				return new SqlProjectionExpression(select, projector, aggregator, projectionExpression.IsElementTableProjection, projectionExpression.DefaultValue);
			}

			return projectionExpression;
		}

		protected virtual Expression VisitTable(SqlTableExpression table)
		{
			return table;
		}

		protected virtual Expression VisitColumn(SqlColumnExpression columnExpression)
		{
			return columnExpression;
		}

		protected virtual Expression VisitFunctionCall(SqlFunctionCallExpression functionCallExpression)
		{
			var newArgs = this.VisitExpressionList(functionCallExpression.Arguments);

			if (newArgs != functionCallExpression.Arguments)
			{
				return new SqlFunctionCallExpression(functionCallExpression.Type, functionCallExpression.Function, newArgs.ToArray());
			}

			return functionCallExpression;
		}


		protected virtual Expression VisitSubquery(SqlSubqueryExpression subquery)
		{
			var select = (SqlSelectExpression)this.Visit(subquery.Select);

			if (select != subquery.Select)
			{
				return new SqlSubqueryExpression(subquery.Type, select);
			}

			return subquery;
		}

		protected virtual Expression VisitAggregate(SqlAggregateExpression sqlAggregate)
		{
			var arg = this.Visit(sqlAggregate.Argument);

			return UpdateAggregate(sqlAggregate, sqlAggregate.Type, sqlAggregate.AggregateType, arg, sqlAggregate.IsDistinct);
		}

		protected static SqlAggregateExpression UpdateAggregate(SqlAggregateExpression sqlAggregate, Type type, SqlAggregateType aggType, Expression arg, bool isDistinct)
		{
			if (type != sqlAggregate.Type || aggType != sqlAggregate.AggregateType || arg != sqlAggregate.Argument || isDistinct != sqlAggregate.IsDistinct)
			{
				return new SqlAggregateExpression(type, aggType, arg, isDistinct);
			}

			return sqlAggregate;
		}

		protected virtual Expression VisitAggregateSubquery(SqlAggregateSubqueryExpression aggregate)
		{
			var e = this.Visit(aggregate.AggregateAsSubquery);

			var subquery = (SqlSubqueryExpression)e;

			if (subquery != aggregate.AggregateAsSubquery)
			{
				return new SqlAggregateSubqueryExpression(aggregate.GroupByAlias, aggregate.AggregateInGroupSelect, subquery);
			}

			return aggregate;
		}

		protected virtual Expression VisitSelect(SqlSelectExpression selectExpression)
		{
			var from = this.VisitSource(selectExpression.From);
			var where = this.Visit(selectExpression.Where);
			var orderBy = this.VisitExpressionList(selectExpression.OrderBy);
			var groupBy = this.VisitExpressionList(selectExpression.GroupBy);
			var skip = this.Visit(selectExpression.Skip);
			var take = this.Visit(selectExpression.Take);
			var columns = this.VisitColumnDeclarations(selectExpression.Columns);

			if (from != selectExpression.From || where != selectExpression.Where || columns != selectExpression.Columns || orderBy != selectExpression.OrderBy || groupBy != selectExpression.GroupBy || take != selectExpression.Take || skip != selectExpression.Skip)
			{
				return new SqlSelectExpression(selectExpression.Type, selectExpression.Alias, columns, from, where, orderBy, groupBy, selectExpression.Distinct, skip, take, selectExpression.ForUpdate, selectExpression.Reverse);
			}

			return selectExpression;
		}

		protected virtual Expression VisitOrderBy(SqlOrderByExpression orderByExpression)
		{
			var newExpression = this.Visit(orderByExpression.Expression);

			if (newExpression != orderByExpression.Expression)
			{
				return new SqlOrderByExpression(orderByExpression.OrderType, newExpression);
			}

			return orderByExpression;
		}

		protected virtual Expression VisitSource(Expression source)
		{
			return this.Visit(source);
		}

		protected virtual Expression VisitProjection(SqlProjectionExpression projection)
		{
			var source = (SqlSelectExpression)this.Visit(projection.Select);

			var projector = this.Visit(projection.Projector);
			var defaulValueExpression = this.Visit(projection.DefaultValue);
			var aggregator = (LambdaExpression)this.Visit(projection.Aggregator);
			var defaultValue = this.Visit(projection.DefaultValue);

			if (source != projection.Select || projector != projection.Projector || defaulValueExpression != projection.DefaultValue || aggregator != projection.Aggregator || defaultValue != projection.DefaultValue)
			{
				return new SqlProjectionExpression(projection.Type, source, projector, aggregator, projection.IsElementTableProjection, projection.DefaultValue);
			}

			return projection;
		}

		protected virtual SqlColumnDeclaration VisitColumnDeclaration(SqlColumnDeclaration sqlColumnDeclaration)
		{
			var e = this.Visit(sqlColumnDeclaration.Expression);

			if (e != sqlColumnDeclaration.Expression)
			{
				return new SqlColumnDeclaration(sqlColumnDeclaration.Name, e);
			}

			return sqlColumnDeclaration;
		}

		protected virtual IReadOnlyList<SqlColumnDeclaration> VisitColumnDeclarations(IReadOnlyList<SqlColumnDeclaration> columns)
		{
			var i = 0;
			List<SqlColumnDeclaration> alternate = null;

			foreach (var column in columns)
			{
				var e = this.Visit(column.Expression);

				if (alternate == null && e != column.Expression)
				{
					alternate = columns.Take(i).ToList();
				}

				alternate?.Add(new SqlColumnDeclaration(column.Name, e));

				i++;
			}

			return alternate != null ? alternate.ToReadOnlyCollection() : columns;
		}

		protected virtual Expression VisitDelete(SqlDeleteExpression deleteExpression)
		{
			var where = this.Visit(deleteExpression.Where);

			if (deleteExpression.Where != where)
			{
				return new SqlDeleteExpression(deleteExpression.Table, deleteExpression.Alias, where);
			}

			return deleteExpression;
		}

		protected virtual Expression VisitColumnDefinition(SqlColumnDefinitionExpression columnDefinitionExpression)
		{
			var constraints = this.VisitExpressionList(columnDefinitionExpression.ConstraintExpressions);

			if (constraints != columnDefinitionExpression.ConstraintExpressions)
			{
				return new SqlColumnDefinitionExpression(columnDefinitionExpression.ColumnName, columnDefinitionExpression.ColumnType, constraints);
			}

			return columnDefinitionExpression;
		}

		protected virtual Expression VisitCreateTable(SqlCreateTableExpression createTableExpression)
		{
			var newTable = (SqlTableExpression)this.Visit(createTableExpression.Table);
			var constraints = this.VisitExpressionList(createTableExpression.TableConstraints);
			var columnDefinitions = this.VisitExpressionList(createTableExpression.ColumnDefinitionExpressions);

			if (newTable != createTableExpression.Table || createTableExpression.TableConstraints != constraints || createTableExpression.ColumnDefinitionExpressions != columnDefinitions)
			{
				return new SqlCreateTableExpression(newTable, false, columnDefinitions, constraints, Enumerable.Empty<SqlTableOption>());
			}
			else
			{
				return createTableExpression;
			}
		}

		protected virtual Expression VisitAlterTable(SqlAlterTableExpression alterTableExpression)
		{
			var newTable = this.Visit(alterTableExpression.Table);
			var newList = this.VisitExpressionList(alterTableExpression.Actions);

			if (newTable != alterTableExpression.Table || newList != alterTableExpression.Actions)
			{
				return new SqlAlterTableExpression(newTable, newList);
			}

			return alterTableExpression;
		}

		protected virtual Expression VisitConstraintAction(SqlConstraintActionExpression actionExpression)
		{
			return actionExpression;
		}

		protected virtual Expression VisitCreateIndex(SqlCreateIndexExpression createIndexExpression)
		{
			return createIndexExpression;
		}

		protected virtual Expression VisitReferencesColumn(SqlReferencesColumnExpression referencesColumnExpression)
		{
			return referencesColumnExpression;
		}

		protected virtual Expression VisitSimpleConstraint(SqlSimpleConstraintExpression simpleConstraintExpression)
		{
			return simpleConstraintExpression;
		}

		protected virtual Expression VisitStatementList(SqlStatementListExpression statementListExpression)
		{
			List<Expression> newStatements = null;

			for (var i = 0; i < statementListExpression.Statements.Count; i++)
			{
				var expression = this.Visit(statementListExpression.Statements[i]);

				if (expression != statementListExpression.Statements[i])
				{
					if (newStatements == null)
					{
						newStatements = new List<Expression>();

						for (var j = 0; j < i; j++)
						{
							newStatements.Add(statementListExpression.Statements[j]);
						}
					}
				}

				if (newStatements != null)
				{
					newStatements.Add(expression);
				}
			}

			return newStatements == null ? statementListExpression : new SqlStatementListExpression(newStatements);
		}

		protected virtual Expression VisitForeignKeyConstraint(SqlForeignKeyConstraintExpression foreignKeyConstraintExpression)
		{
			var referencesColumnExpression = (SqlReferencesColumnExpression)this.Visit(foreignKeyConstraintExpression.ReferencesColumnExpression);

			if (referencesColumnExpression != foreignKeyConstraintExpression.ReferencesColumnExpression)
			{
				return new SqlForeignKeyConstraintExpression(foreignKeyConstraintExpression.ConstraintName, foreignKeyConstraintExpression.ColumnNames, referencesColumnExpression);
			}
			else
			{
				return foreignKeyConstraintExpression;
			}
		}

		protected virtual Expression VisitIndexedColumn(SqlIndexedColumnExpression indexedColumnExpression)
		{
			var newColumn = (SqlColumnExpression)this.Visit(indexedColumnExpression.Column);

			if (newColumn != indexedColumnExpression.Column)
			{
				return new SqlIndexedColumnExpression(newColumn, indexedColumnExpression.SortOrder, indexedColumnExpression.LowercaseIndex);
			}

			return indexedColumnExpression;
		}

		protected virtual Expression VisitQueryArgument(SqlQueryArgumentExpression expression)
		{
			return expression;
		}
	}
}
