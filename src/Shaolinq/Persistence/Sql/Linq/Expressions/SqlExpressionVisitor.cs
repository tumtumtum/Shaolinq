// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;

namespace Shaolinq.Persistence.Sql.Linq.Expressions
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

			switch ((SqlExpressionType) expression.NodeType)
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
					return this.VisitAggregate((SqlAggregateExpression)expression);
				case SqlExpressionType.Subquery:
					return this.VisitSubquery((SqlSubqueryExpression)expression);
				case SqlExpressionType.AggregateSubquery:
					return this.VisitAggregateSubquery((SqlAggregateSubqueryExpression)expression);
				case SqlExpressionType.ObjectOperand:
					return this.VisitObjectOperand((SqlObjectOperand)expression);
				case SqlExpressionType.Tuple:
					return this.VisitTuple((SqlTupleExpression)expression);
				case SqlExpressionType.Delete:
					return this.VisitDelete((SqlDeleteExpression)expression);
				case SqlExpressionType.AlterTable:
					return this.VisitAlterTable((SqlAlterTableExpression)expression);
				case SqlExpressionType.ColumnDefinition:
					return this.VisitColumnDefinition((SqlColumnDefinitionExpression)expression);
				case SqlExpressionType.CreateIndex:
					return this.VisitCreateIndex((SqlCreateIndexExpression)expression);
				case SqlExpressionType.CreateTable:
					return this.VisitCreateTable((SqlCreateTableExpression)expression);
				case SqlExpressionType.ForeignKeyConstraint:
					return this.VisitForeignKeyConstraint((SqlForeignKeyConstraintExpression)expression);
				case SqlExpressionType.ReferencesColumn:
					return this.VisitReferencesColumn((SqlReferencesColumnExpression)expression);
				case SqlExpressionType.SimpleConstraint:
					return this.VisitSimpleConstraint((SqlSimpleConstraintExpression)expression);
				default:
					return base.Visit(expression);
			}
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

			return new SqlConstantPlaceholderExpression(constantPlaceholder.Index, (ConstantExpression)result);
		}

		protected virtual Expression VisitObjectOperand(SqlObjectOperand objectOperand)
		{
			var expression = VisitExpressionList(objectOperand.ExpressionsInOrder);

			if (objectOperand.ExpressionsInOrder != expression)
			{
				var newPropertyNames = new List<string>();
				
				foreach (var operandExpression in objectOperand.ExpressionsInOrder)
				{
					var key = objectOperand.PropertyNamesByExpression[operandExpression];

					newPropertyNames.Add(key);
				}

				return new SqlObjectOperand(objectOperand.Type, expression, newPropertyNames);
			}
			else
			{
				return objectOperand;
			}
		}

		protected virtual Expression VisitJoin(SqlJoinExpression join)
		{
			var left = this.Visit(join.Left);
			var right = this.Visit(join.Right);
			var condition = this.Visit(join.Condition);

			if (left != join.Left || right != join.Right || condition != join.Condition)
			{
				return new SqlJoinExpression(join.Type, join.JoinType, left, right, condition);
			}

			return join;
		}

        protected static SqlProjectionExpression UpdateProjection(SqlProjectionExpression projectionExpression, SqlSelectExpression select, Expression projector, LambdaExpression aggregator)
		{
			if (select != projectionExpression.Select || projector != projectionExpression.Projector || aggregator != projectionExpression.Aggregator)
			{
				return new SqlProjectionExpression(select, projector, aggregator, projectionExpression.IsElementTableProjection, projectionExpression.SelectFirstType, projectionExpression.DefaultValueExpression);
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
			var from = VisitSource(selectExpression.From);
			var where = Visit(selectExpression.Where);
            
			var orderBy = this.VisitOrderBy(selectExpression.OrderBy);
			var groupBy = this.VisitExpressionList(selectExpression.GroupBy);
			var skip = this.Visit(selectExpression.Skip);
			var take = this.Visit(selectExpression.Take);
			var columns = VisitColumnDeclarations(selectExpression.Columns);
            
			if (from != selectExpression.From || where != selectExpression.Where || columns != selectExpression.Columns || orderBy != selectExpression.OrderBy || groupBy != selectExpression.GroupBy || take != selectExpression.Take || skip != selectExpression.Skip)
			{
				return new SqlSelectExpression(selectExpression.Type, selectExpression.Alias, columns, from, where, orderBy, groupBy, selectExpression.Distinct, skip, take, selectExpression.ForUpdate);
			}

			return selectExpression;
		}

		protected virtual ReadOnlyCollection<SqlOrderByExpression> VisitOrderBy(ReadOnlyCollection<SqlOrderByExpression> expressions)
		{
			if (expressions != null)
			{
				List<SqlOrderByExpression> alternate = null;

				for (int i = 0, n = expressions.Count; i < n; i++)
				{
					var expr = expressions[i];
					var e = this.Visit(expr.Expression);

					if (alternate == null && e != expr.Expression)
					{
						alternate = expressions.Take(i).ToList();
					}

					if (alternate != null)
					{
						alternate.Add(new SqlOrderByExpression(expr.OrderType, e));
					}
				}

				if (alternate != null)
				{
					return alternate.AsReadOnly();
				}
			}

			return expressions;
		}
        
		protected virtual Expression VisitSource(Expression source)
		{
			return Visit(source);
		}

		protected virtual Expression VisitProjection(SqlProjectionExpression projection)
		{
			var source = (SqlSelectExpression)Visit(projection.Select);

			var projector = Visit(projection.Projector);

			var aggregator = (LambdaExpression)Visit(projection.Aggregator);

			if (source != projection.Select || projector != projection.Projector
				|| aggregator != projection.Aggregator)
			{
				return new SqlProjectionExpression(source, projector, aggregator, projection.IsElementTableProjection, projection.SelectFirstType, null);
			}

			return projection;
		}

		protected virtual ReadOnlyCollection<SqlColumnDeclaration> VisitColumnDeclarations(ReadOnlyCollection<SqlColumnDeclaration> columns)
		{
			List<SqlColumnDeclaration> alternate = null;

			for (int i = 0, n = columns.Count; i < n; i++)
			{
				var column = columns[i];
				var e = Visit(column.Expression);

				if (alternate == null && e != column.Expression)
				{
					alternate = columns.Take(i).ToList();
				}

				if (alternate != null)
				{
					alternate.Add(new SqlColumnDeclaration(column.Name, e));
				}
			}

			if (alternate != null)
			{
				return alternate.AsReadOnly();
			}

			return columns;
		}

		protected virtual Expression VisitDelete(SqlDeleteExpression deleteExpression)
		{
			var where = Visit(deleteExpression.Where);

			if (deleteExpression.Where != where)
			{
				return new SqlDeleteExpression(deleteExpression.TableName, deleteExpression.Alias, where);
			}

			return deleteExpression;
		}

		protected virtual Expression VisitColumnDefinition(SqlColumnDefinitionExpression columnDefinitionExpression)
		{
			var constraints = this.VisitExpressionList(columnDefinitionExpression.ConstraintExpressions);

			if (constraints != columnDefinitionExpression.ConstraintExpressions)
			{
				return new SqlColumnDefinitionExpression(columnDefinitionExpression.ColumnName, columnDefinitionExpression.ColumnTypeName, constraints);
			}

			return columnDefinitionExpression;
		}

		protected virtual Expression VisitCreateTable(SqlCreateTableExpression createTableExpression)
		{
			var constraints = VisitExpressionList(createTableExpression.TableConstraints);
			var columnDefinitions = VisitExpressionList(createTableExpression.TableConstraints);

			if (createTableExpression.TableConstraints != constraints || createTableExpression.ColumnDefinitionExpressions != columnDefinitions)
			{
				return new SqlCreateTableExpression(createTableExpression.TableName, columnDefinitions, constraints);
			}
			else
			{
				return createTableExpression;
			}
		}

		protected virtual Expression VisitAlterTable(SqlAlterTableExpression alterTableExpression)
		{
			return alterTableExpression;
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

		protected virtual Expression VisitForeignKeyConstraint(SqlForeignKeyConstraintExpression foreignKeyConstraintExpression)
		{
			var referencesColumnExpression = (SqlReferencesColumnExpression)this.Visit(foreignKeyConstraintExpression.ReferencesColumnExpression);

			if (referencesColumnExpression != foreignKeyConstraintExpression.ReferencesColumnExpression)
			{
				return new SqlForeignKeyConstraintExpression(foreignKeyConstraintExpression.ColumnName, referencesColumnExpression);
			}
			else
			{
				return foreignKeyConstraintExpression;
			}
		}
	}
}
