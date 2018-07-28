// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)

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
				return VisitConstantPlaceholder((SqlConstantPlaceholderExpression)expression);
			case SqlExpressionType.Table:
				return VisitTable((SqlTableExpression)expression);
			case SqlExpressionType.Column:
				return VisitColumn((SqlColumnExpression)expression);
			case SqlExpressionType.Select:
				return VisitSelect((SqlSelectExpression)expression);
			case SqlExpressionType.Join:
				return VisitJoin((SqlJoinExpression)expression);
			case SqlExpressionType.Projection:
				return VisitProjection((SqlProjectionExpression)expression);
			case SqlExpressionType.FunctionCall:
				return VisitFunctionCall((SqlFunctionCallExpression)expression);
			case SqlExpressionType.Aggregate:
				return VisitAggregate((SqlAggregateExpression)expression);
			case SqlExpressionType.Subquery:
				return VisitSubquery((SqlSubqueryExpression)expression);
			case SqlExpressionType.AggregateSubquery:
				return VisitAggregateSubquery((SqlAggregateSubqueryExpression)expression);
			case SqlExpressionType.ObjectReference:
				return VisitObjectReference((SqlObjectReferenceExpression)expression);
			case SqlExpressionType.OrderBy:
				return VisitOrderBy((SqlOrderByExpression)expression);
			case SqlExpressionType.Tuple:
				return VisitTuple((SqlTupleExpression)expression);
			case SqlExpressionType.Delete:
				return VisitDelete((SqlDeleteExpression)expression);
			case SqlExpressionType.AlterTable:
				return VisitAlterTable((SqlAlterTableExpression)expression);
			case SqlExpressionType.ConstraintAction:
				return VisitConstraintAction((SqlConstraintActionExpression)expression);
			case SqlExpressionType.ColumnDefinition:
				return VisitColumnDefinition((SqlColumnDefinitionExpression)expression);
			case SqlExpressionType.CreateIndex:
				return VisitCreateIndex((SqlCreateIndexExpression)expression);
			case SqlExpressionType.IndexedColumn:
				return VisitIndexedColumn((SqlIndexedColumnExpression)expression);
			case SqlExpressionType.CreateTable:
				return VisitCreateTable((SqlCreateTableExpression)expression);
			case SqlExpressionType.Constraint:
				return VisitConstraint((SqlConstraintExpression)expression);
			case SqlExpressionType.References:
				return VisitReferences((SqlReferencesExpression)expression);
			case SqlExpressionType.StatementList:
				return VisitStatementList((SqlStatementListExpression)expression);
			case SqlExpressionType.InsertInto:
				return VisitInsertInto((SqlInsertIntoExpression)expression);
			case SqlExpressionType.Update:
				return VisitUpdate((SqlUpdateExpression)expression);
			case SqlExpressionType.Assign:
				return VisitAssign((SqlAssignExpression)expression);
			case SqlExpressionType.CreateType:
				return VisitCreateType((SqlCreateTypeExpression)expression);
			case SqlExpressionType.Type:
				return VisitType((SqlTypeExpression)expression);
			case SqlExpressionType.EnumDefinition:
				return VisitEnumDefinition((SqlEnumDefinitionExpression)expression);
			case SqlExpressionType.Pragma:
				return VisitPragma((SqlPragmaExpression)expression);
			case SqlExpressionType.SetCommand:
				return VisitSetCommand((SqlSetCommandExpression)expression);
			case SqlExpressionType.Over:
				return VisitOver((SqlOverExpression)expression);
			case SqlExpressionType.Scalar:
				return VisitScalar((SqlScalarExpression)expression);
			case SqlExpressionType.Union:
				return VisitUnion((SqlUnionExpression)expression);
			case SqlExpressionType.TableHint:
				return VisitTableHint((SqlTableHintExpression)expression);
			case SqlExpressionType.Keyword:
				return VisitKeyword((SqlKeywordExpression)expression);
			case SqlExpressionType.VariableDeclaration:
				return VisitVariableDeclaration((SqlVariableDeclarationExpression)expression);
			case SqlExpressionType.Declare:
				return VisitDeclare((SqlDeclareExpression)expression);
			case SqlExpressionType.OrganizationIndex:
			return VisitOrganizationIndex((SqlOrganizationIndexExpression)expression);
			default:
				return base.Visit(expression);
			}
		}

		protected virtual Expression VisitOrganizationIndex(SqlOrganizationIndexExpression expression)
		{
			var columns = VisitExpressionList(expression.Columns);
			var includedColumns = VisitExpressionList(expression.IncludedColumns);

			if (columns != expression.Columns || includedColumns != expression.IncludedColumns)
			{
				return expression.ChangeColumns(columns, includedColumns);
			}

			return expression;
		}

		protected virtual Expression VisitReferences(SqlReferencesExpression expression)
		{
			var table = (SqlTableExpression)Visit(expression.ReferencedTable);
			
			if (table != expression.ReferencedTable)
			{
				return expression.ChangeReferencedTable(table);
			}

			return expression;
		}

		protected virtual Expression VisitVariableDeclaration(SqlVariableDeclarationExpression expression)
		{
			return expression;
		}

		protected virtual Expression VisitDeclare(SqlDeclareExpression expression)
		{
			var variableDeclarations = VisitExpressionList(expression.VariableDeclarations);

			if (variableDeclarations != expression.VariableDeclarations)
			{
				return new SqlDeclareExpression(variableDeclarations);
			}

			return expression;
		}

		protected virtual Expression VisitKeyword(SqlKeywordExpression expression)
		{
			return expression;
		}

		protected virtual Expression VisitTableHint(SqlTableHintExpression expression)
		{
			return expression;
		}

		protected virtual Expression VisitScalar(SqlScalarExpression expression)
		{
			var select = Visit(expression.Select);

			if (select != expression.Select)
			{
				return expression.ChangeSelect((SqlSelectExpression)select);
			}

			return expression;
		}

		protected virtual Expression VisitOver(SqlOverExpression expression)
		{
			var source = Visit(expression.Source);
			var orderBy = VisitExpressionList(expression.OrderBy);

			if (source != expression.Source || orderBy != expression.OrderBy)
			{
				return new SqlOverExpression(source, orderBy);
			}

			return expression;
		}

		protected virtual Expression VisitSetCommand(SqlSetCommandExpression expression)
		{
			var target = Visit(expression.Target);
			var arguments = VisitExpressionList(expression.Arguments);

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
			var newSqlType = Visit(expression.SqlType);
			var newAsExpression = Visit(expression.AsExpression);

			if (newSqlType != expression.SqlType || newAsExpression != expression.AsExpression)
			{
				return new SqlCreateTypeExpression(newSqlType, newAsExpression, false);
			}

			return expression;
		}

		protected virtual Expression VisitAssign(SqlAssignExpression expression)
		{
			var newTarget = Visit(expression.Target);
			var newValue = Visit(expression.Value);

			if (newTarget != expression.Target || newValue != expression.Value)
			{
				return new SqlAssignExpression(newTarget, newValue);
			}

			return expression;
		}

		protected virtual Expression VisitUpdate(SqlUpdateExpression expression)
		{
			var newSource = Visit(expression.Source);
			var newWhere = Visit(expression.Where);
			var newAssignments = VisitExpressionList(expression.Assignments);

			if (newSource != expression.Source || newWhere != expression.Where || newAssignments != expression.Assignments)
			{
				return new SqlUpdateExpression(newSource, newAssignments, newWhere, expression.RequiresIdentityInsert);
			}

			return expression;
		}

		protected virtual Expression VisitInsertInto(SqlInsertIntoExpression expression)
		{
			var source = VisitSource(expression.Source);
			var valueExpressions = VisitExpressionList(expression.ValueExpressions);

			if (source != expression.Source || valueExpressions != expression.ValueExpressions)
			{
				return new SqlInsertIntoExpression(source, expression.ColumnNames, expression.ReturningAutoIncrementColumnNames, valueExpressions);
			}

			return expression;
		}

		protected virtual Expression VisitTuple(SqlTupleExpression tupleExpression)
		{
			var expressions = VisitExpressionList(tupleExpression.SubExpressions);

			if (tupleExpression.SubExpressions != expressions)
			{
				return new SqlTupleExpression(expressions, tupleExpression.Type);
			}

			return tupleExpression;
		}

		protected virtual Expression VisitConstantPlaceholder(SqlConstantPlaceholderExpression constantPlaceholder)
		{
			var result = VisitConstant(constantPlaceholder.ConstantExpression);

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
			var newBindings = VisitBindingList(objectReferenceExpression.Bindings);

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
			var left = Visit(join.Left);
			var right = Visit(join.Right);
			var condition = Visit(join.JoinCondition);

			if (condition == null)
			{
				Visit(join.JoinCondition);
			}

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
			var newArgs = VisitExpressionList(functionCallExpression.Arguments);

			if (newArgs != functionCallExpression.Arguments)
			{
				return new SqlFunctionCallExpression(functionCallExpression.Type, functionCallExpression.Function, newArgs.ToArray());
			}

			return functionCallExpression;
		}


		protected virtual Expression VisitSubquery(SqlSubqueryExpression subquery)
		{
			var select = (SqlSelectExpression)Visit(subquery.Select);

			if (select != subquery.Select)
			{
				return new SqlSubqueryExpression(subquery.Type, select);
			}

			return subquery;
		}

		protected virtual Expression VisitAggregate(SqlAggregateExpression sqlAggregate)
		{
			var arg = Visit(sqlAggregate.Argument);

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
			var e = Visit(aggregate.AggregateAsSubquery);

			var subquery = (SqlSubqueryExpression)e;

			if (subquery != aggregate.AggregateAsSubquery)
			{
				return new SqlAggregateSubqueryExpression(aggregate.GroupByAlias, aggregate.AggregateInGroupSelect, subquery);
			}

			return aggregate;
		}

		protected virtual Expression VisitSelect(SqlSelectExpression selectExpression)
		{
			var from = VisitSource(selectExpression.From);
			var where = Visit(selectExpression.Where);
			var orderBy = VisitExpressionList(selectExpression.OrderBy);
			var groupBy = VisitExpressionList(selectExpression.GroupBy);
			var skip = Visit(selectExpression.Skip);
			var take = Visit(selectExpression.Take);
			var columns = VisitColumnDeclarations(selectExpression.Columns);
		    var into = Visit(selectExpression.Into);

			if (from != selectExpression.From || where != selectExpression.Where || columns != selectExpression.Columns || orderBy != selectExpression.OrderBy || groupBy != selectExpression.GroupBy || take != selectExpression.Take || skip != selectExpression.Skip || into != selectExpression.Into)
			{
				return new SqlSelectExpression(selectExpression.Type, selectExpression.Alias, columns, from, where, orderBy, groupBy, selectExpression.Distinct, skip, take, selectExpression.ForUpdate, selectExpression.Reverse, into);
			}

			return selectExpression;
		}

		protected virtual Expression VisitOrderBy(SqlOrderByExpression orderByExpression)
		{
			var newExpression = Visit(orderByExpression.Expression);

			if (newExpression != orderByExpression.Expression)
			{
				return new SqlOrderByExpression(orderByExpression.OrderType, newExpression);
			}

			return orderByExpression;
		}

		protected virtual Expression VisitSource(Expression source)
		{
			return Visit(source);
		}

		protected virtual Expression VisitProjection(SqlProjectionExpression projection)
		{	
			var source = (SqlSelectExpression)Visit(projection.Select);

			var projector = Visit(projection.Projector);
			var defaulValueExpression = Visit(projection.DefaultValue);
			var aggregator = (LambdaExpression)Visit(projection.Aggregator);
			var defaultValue = Visit(projection.DefaultValue);

			if (source != projection.Select || projector != projection.Projector || defaulValueExpression != projection.DefaultValue || aggregator != projection.Aggregator || defaultValue != projection.DefaultValue)
			{
				return new SqlProjectionExpression(projection.Type, source, projector, aggregator, projection.IsElementTableProjection, projection.DefaultValue);
			}

			return projection;
		}

		protected virtual SqlColumnDeclaration VisitColumnDeclaration(SqlColumnDeclaration sqlColumnDeclaration)
		{
			var e = Visit(sqlColumnDeclaration.Expression);

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
				var e = Visit(column.Expression);

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
			var source = Visit(deleteExpression.Source);
			var where = Visit(deleteExpression.Where);

			if (deleteExpression.Source != source || deleteExpression.Where != where)
			{
				return new SqlDeleteExpression(source, where);
			}

			return deleteExpression;
		}

		protected virtual Expression VisitColumnDefinition(SqlColumnDefinitionExpression columnDefinitionExpression)
		{
			var constraints = VisitExpressionList(columnDefinitionExpression.ConstraintExpressions);

			if (constraints != columnDefinitionExpression.ConstraintExpressions)
			{
				return new SqlColumnDefinitionExpression(columnDefinitionExpression.ColumnName, columnDefinitionExpression.ColumnType, constraints);
			}

			return columnDefinitionExpression;
		}

		protected virtual Expression VisitCreateTable(SqlCreateTableExpression createTableExpression)
		{
			var newTable = (SqlTableExpression)Visit(createTableExpression.Table);
			var constraints = VisitExpressionList(createTableExpression.TableConstraints);
			var columnDefinitions = VisitExpressionList(createTableExpression.ColumnDefinitionExpressions);
			var organizationIndex = (SqlOrganizationIndexExpression)Visit(createTableExpression.OrganizationIndex);

			if (newTable != createTableExpression.Table || createTableExpression.TableConstraints != constraints || createTableExpression.ColumnDefinitionExpressions != columnDefinitions || createTableExpression.OrganizationIndex != organizationIndex)
			{
				return new SqlCreateTableExpression(newTable, false, columnDefinitions, constraints, organizationIndex, createTableExpression.TableOptions);
			}
			else
			{
				return createTableExpression;
			}
		}

		protected virtual Expression VisitAlterTable(SqlAlterTableExpression alterTableExpression)
		{
			var newTable = Visit(alterTableExpression.Table);
			var newList = VisitExpressionList(alterTableExpression.ConstraintActions);

			if (newTable != alterTableExpression.Table || newList != alterTableExpression.ConstraintActions)
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
			var table = (SqlTableExpression)Visit(createIndexExpression.Table);
			var columns = VisitExpressionList(createIndexExpression.Columns);
			var includedColumns = VisitExpressionList(createIndexExpression.IncludedColumns);
			var where = Visit(createIndexExpression.Where);

			if (columns != createIndexExpression.Columns
				|| includedColumns != createIndexExpression.IncludedColumns
				|| table != createIndexExpression.Table
				|| where != createIndexExpression.Where)
			{
				return new SqlCreateIndexExpression(createIndexExpression.IndexName, table, createIndexExpression.Unique, createIndexExpression.IndexType, createIndexExpression.IfNotExist, columns, includedColumns, where, createIndexExpression.Clustered);
			}

			return createIndexExpression;
		}

		protected virtual Expression VisitStatementList(SqlStatementListExpression statementListExpression)
		{
			List<Expression> newStatements = null;

			for (var i = 0; i < statementListExpression.Statements.Count; i++)
			{
				var expression = Visit(statementListExpression.Statements[i]);

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

				newStatements?.Add(expression);
			}

			return newStatements == null ? statementListExpression : new SqlStatementListExpression(newStatements);
		}

		protected virtual Expression VisitConstraint(SqlConstraintExpression expression)
		{
			var referencesExpression = (SqlReferencesExpression)Visit(expression.ReferencesExpression);

			if (referencesExpression != expression.ReferencesExpression)
			{
				return expression.ChangeReferences(referencesExpression);
			}
			else
			{
				return expression;
			}
		}

		protected virtual Expression VisitIndexedColumn(SqlIndexedColumnExpression indexedColumnExpression)
		{
			var newColumn = (SqlColumnExpression)Visit(indexedColumnExpression.Column);

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

		protected virtual Expression VisitUnion(SqlUnionExpression expression)
		{
			var left = Visit(expression.Left);
			var right = Visit(expression.Right);

			if (left != expression.Left || right != expression.Right)
			{
				return new SqlUnionExpression(expression.Type, expression.Alias, left, right, expression.UnionAll);
			}

			return expression;
		}
	}
}
