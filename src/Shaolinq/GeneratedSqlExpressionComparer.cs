#pragma warning disable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Shaolinq.Persistence.Linq.Expressions
{
    public partial class SqlExpressionComparer
    {
        protected override Expression VisitReferences(SqlReferencesExpression expression)
        {
            SqlReferencesExpression current;
            if (!TryGetCurrent(expression, out current))
            {
                return expression;
            }

            if (!(this.result &= current.Deferrability == expression.Deferrability))
            {
                return expression;
            }

            if (!(this.result &= current.OnDeleteAction == expression.OnDeleteAction))
            {
                return expression;
            }

            if (!(this.result &= current.OnUpdateAction == expression.OnUpdateAction))
            {
                return expression;
            }

            if (!(this.result &= current.NodeType == expression.NodeType))
            {
                return expression;
            }

            if (!(this.result &= current.Type == expression.Type))
            {
                return expression;
            }

            this.currentObject = current.ReferencedTable;
            this.VisitTable(expression.ReferencedTable);
            if (!this.result)
            {
                return expression;
            }

            this.currentObject = current.ReferencedColumnNames;
            this.VisitObjectList(expression.ReferencedColumnNames);
            if (!this.result)
            {
                return expression;
            }

            this.currentObject = current;
            return expression;
        }

        protected override Expression VisitVariableDeclaration(SqlVariableDeclarationExpression expression)
        {
            SqlVariableDeclarationExpression current;
            if (!TryGetCurrent(expression, out current))
            {
                return expression;
            }

            if (!(this.result &= object.Equals(current.Name, expression.Name)))
            {
                return expression;
            }

            if (!(this.result &= current.NodeType == expression.NodeType))
            {
                return expression;
            }

            if (!(this.result &= current.Type == expression.Type))
            {
                return expression;
            }

            this.currentObject = current;
            return expression;
        }

        protected override Expression VisitDeclare(SqlDeclareExpression expression)
        {
            SqlDeclareExpression current;
            if (!TryGetCurrent(expression, out current))
            {
                return expression;
            }

            if (!(this.result &= current.Type == expression.Type))
            {
                return expression;
            }

            if (!(this.result &= current.NodeType == expression.NodeType))
            {
                return expression;
            }

            this.currentObject = current.VariableDeclarations;
            this.VisitExpressionList(expression.VariableDeclarations);
            if (!this.result)
            {
                return expression;
            }

            this.currentObject = current;
            return expression;
        }

        protected override Expression VisitKeyword(SqlKeywordExpression expression)
        {
            SqlKeywordExpression current;
            if (!TryGetCurrent(expression, out current))
            {
                return expression;
            }

            if (!(this.result &= object.Equals(current.Name, expression.Name)))
            {
                return expression;
            }

            if (!(this.result &= current.NodeType == expression.NodeType))
            {
                return expression;
            }

            if (!(this.result &= current.Type == expression.Type))
            {
                return expression;
            }

            this.currentObject = current;
            return expression;
        }

        protected override Expression VisitTableHint(SqlTableHintExpression expression)
        {
            SqlTableHintExpression current;
            if (!TryGetCurrent(expression, out current))
            {
                return expression;
            }

            if (!(this.result &= current.TableLock == expression.TableLock))
            {
                return expression;
            }

            if (!(this.result &= current.NodeType == expression.NodeType))
            {
                return expression;
            }

            if (!(this.result &= current.Type == expression.Type))
            {
                return expression;
            }

            this.currentObject = current;
            return expression;
        }

        protected override Expression VisitScalar(SqlScalarExpression expression)
        {
            SqlScalarExpression current;
            if (!TryGetCurrent(expression, out current))
            {
                return expression;
            }

            if (!(this.result &= current.NodeType == expression.NodeType))
            {
                return expression;
            }

            if (!(this.result &= current.Type == expression.Type))
            {
                return expression;
            }

            this.currentObject = current.Select;
            this.VisitSelect(expression.Select);
            if (!this.result)
            {
                return expression;
            }

            this.currentObject = current;
            return expression;
        }

        protected override Expression VisitOver(SqlOverExpression expression)
        {
            SqlOverExpression current;
            if (!TryGetCurrent(expression, out current))
            {
                return expression;
            }

            if (!(this.result &= current.NodeType == expression.NodeType))
            {
                return expression;
            }

            if (!(this.result &= current.Type == expression.Type))
            {
                return expression;
            }

            this.currentObject = current.Source;
            this.Visit(expression.Source);
            if (!this.result)
            {
                return expression;
            }

            this.currentObject = current.OrderBy;
            this.VisitExpressionList(expression.OrderBy);
            if (!this.result)
            {
                return expression;
            }

            this.currentObject = current;
            return expression;
        }

        protected override Expression VisitSetCommand(SqlSetCommandExpression expression)
        {
            SqlSetCommandExpression current;
            if (!TryGetCurrent(expression, out current))
            {
                return expression;
            }

            if (!(this.result &= object.Equals(current.ConfigurationParameter, expression.ConfigurationParameter)))
            {
                return expression;
            }

            if (!(this.result &= current.NodeType == expression.NodeType))
            {
                return expression;
            }

            if (!(this.result &= current.Type == expression.Type))
            {
                return expression;
            }

            this.currentObject = current.Target;
            this.Visit(expression.Target);
            if (!this.result)
            {
                return expression;
            }

            this.currentObject = current.Arguments;
            this.VisitExpressionList(expression.Arguments);
            if (!this.result)
            {
                return expression;
            }

            this.currentObject = current;
            return expression;
        }

        protected override Expression VisitPragma(SqlPragmaExpression expression)
        {
            SqlPragmaExpression current;
            if (!TryGetCurrent(expression, out current))
            {
                return expression;
            }

            if (!(this.result &= object.Equals(current.Directive, expression.Directive)))
            {
                return expression;
            }

            if (!(this.result &= current.NodeType == expression.NodeType))
            {
                return expression;
            }

            if (!(this.result &= current.Type == expression.Type))
            {
                return expression;
            }

            this.currentObject = current;
            return expression;
        }

        protected override Expression VisitEnumDefinition(SqlEnumDefinitionExpression expression)
        {
            SqlEnumDefinitionExpression current;
            if (!TryGetCurrent(expression, out current))
            {
                return expression;
            }

            if (!(this.result &= current.NodeType == expression.NodeType))
            {
                return expression;
            }

            if (!(this.result &= current.Type == expression.Type))
            {
                return expression;
            }

            this.currentObject = current.Labels;
            this.VisitObjectList(expression.Labels);
            if (!this.result)
            {
                return expression;
            }

            this.currentObject = current;
            return expression;
        }

        protected override Expression VisitType(SqlTypeExpression expression)
        {
            SqlTypeExpression current;
            if (!TryGetCurrent(expression, out current))
            {
                return expression;
            }

            if (!(this.result &= object.Equals(current.TypeName, expression.TypeName)))
            {
                return expression;
            }

            if (!(this.result &= current.UserDefinedType == expression.UserDefinedType))
            {
                return expression;
            }

            if (!(this.result &= current.NodeType == expression.NodeType))
            {
                return expression;
            }

            if (!(this.result &= current.Type == expression.Type))
            {
                return expression;
            }

            this.currentObject = current;
            return expression;
        }

        protected override Expression VisitCreateType(SqlCreateTypeExpression expression)
        {
            SqlCreateTypeExpression current;
            if (!TryGetCurrent(expression, out current))
            {
                return expression;
            }

            if (!(this.result &= current.IfNotExist == expression.IfNotExist))
            {
                return expression;
            }

            if (!(this.result &= current.NodeType == expression.NodeType))
            {
                return expression;
            }

            if (!(this.result &= current.Type == expression.Type))
            {
                return expression;
            }

            this.currentObject = current.SqlType;
            this.Visit(expression.SqlType);
            if (!this.result)
            {
                return expression;
            }

            this.currentObject = current.AsExpression;
            this.Visit(expression.AsExpression);
            if (!this.result)
            {
                return expression;
            }

            this.currentObject = current;
            return expression;
        }

        protected override Expression VisitAssign(SqlAssignExpression expression)
        {
            SqlAssignExpression current;
            if (!TryGetCurrent(expression, out current))
            {
                return expression;
            }

            if (!(this.result &= current.NodeType == expression.NodeType))
            {
                return expression;
            }

            if (!(this.result &= current.Type == expression.Type))
            {
                return expression;
            }

            this.currentObject = current.Target;
            this.Visit(expression.Target);
            if (!this.result)
            {
                return expression;
            }

            this.currentObject = current.Value;
            this.Visit(expression.Value);
            if (!this.result)
            {
                return expression;
            }

            this.currentObject = current;
            return expression;
        }

        protected override Expression VisitUpdate(SqlUpdateExpression expression)
        {
            SqlUpdateExpression current;
            if (!TryGetCurrent(expression, out current))
            {
                return expression;
            }

            if (!(this.result &= current.RequiresIdentityInsert == expression.RequiresIdentityInsert))
            {
                return expression;
            }

            if (!(this.result &= current.NodeType == expression.NodeType))
            {
                return expression;
            }

            if (!(this.result &= current.Type == expression.Type))
            {
                return expression;
            }

            this.currentObject = current.Source;
            this.Visit(expression.Source);
            if (!this.result)
            {
                return expression;
            }

            this.currentObject = current.Where;
            this.Visit(expression.Where);
            if (!this.result)
            {
                return expression;
            }

            this.currentObject = current.Assignments;
            this.VisitExpressionList(expression.Assignments);
            if (!this.result)
            {
                return expression;
            }

            this.currentObject = current;
            return expression;
        }

        protected override Expression VisitInsertInto(SqlInsertIntoExpression expression)
        {
            SqlInsertIntoExpression current;
            if (!TryGetCurrent(expression, out current))
            {
                return expression;
            }

            if (!(this.result &= current.RequiresIdentityInsert == expression.RequiresIdentityInsert))
            {
                return expression;
            }

            if (!(this.result &= current.NodeType == expression.NodeType))
            {
                return expression;
            }

            if (!(this.result &= current.Type == expression.Type))
            {
                return expression;
            }

            this.currentObject = current.Source;
            this.Visit(expression.Source);
            if (!this.result)
            {
                return expression;
            }

            this.currentObject = current.WithExpression;
            this.Visit(expression.WithExpression);
            if (!this.result)
            {
                return expression;
            }

            this.currentObject = current.ValuesExpression;
            this.Visit(expression.ValuesExpression);
            if (!this.result)
            {
                return expression;
            }

            this.currentObject = current.ColumnNames;
            this.VisitObjectList(expression.ColumnNames);
            if (!this.result)
            {
                return expression;
            }

            this.currentObject = current.ValueExpressions;
            this.VisitExpressionList(expression.ValueExpressions);
            if (!this.result)
            {
                return expression;
            }

            this.currentObject = current.ReturningAutoIncrementColumnNames;
            this.VisitObjectList(expression.ReturningAutoIncrementColumnNames);
            if (!this.result)
            {
                return expression;
            }

            this.currentObject = current;
            return expression;
        }

        protected override Expression VisitTuple(SqlTupleExpression expression)
        {
            SqlTupleExpression current;
            if (!TryGetCurrent(expression, out current))
            {
                return expression;
            }

            if (!(this.result &= current.Type == expression.Type))
            {
                return expression;
            }

            if (!(this.result &= current.NodeType == expression.NodeType))
            {
                return expression;
            }

            this.currentObject = current.SubExpressions;
            this.VisitExpressionList(expression.SubExpressions);
            if (!this.result)
            {
                return expression;
            }

            this.currentObject = current;
            return expression;
        }

        protected override Expression VisitObjectReference(SqlObjectReferenceExpression expression)
        {
            SqlObjectReferenceExpression current;
            if (!TryGetCurrent(expression, out current))
            {
                return expression;
            }

            if (!(this.result &= current.NodeType == expression.NodeType))
            {
                return expression;
            }

            if (!(this.result &= current.Type == expression.Type))
            {
                return expression;
            }

            this.currentObject = current.Bindings;
            this.VisitBindingList(expression.Bindings);
            if (!this.result)
            {
                return expression;
            }

            this.currentObject = current;
            return expression;
        }

        protected override Expression VisitJoin(SqlJoinExpression expression)
        {
            SqlJoinExpression current;
            if (!TryGetCurrent(expression, out current))
            {
                return expression;
            }

            if (!(this.result &= current.JoinType == expression.JoinType))
            {
                return expression;
            }

            if (!(this.result &= current.NodeType == expression.NodeType))
            {
                return expression;
            }

            if (!(this.result &= current.Type == expression.Type))
            {
                return expression;
            }

            this.currentObject = current.Left;
            this.Visit(expression.Left);
            if (!this.result)
            {
                return expression;
            }

            this.currentObject = current.Right;
            this.Visit(expression.Right);
            if (!this.result)
            {
                return expression;
            }

            this.currentObject = current.JoinCondition;
            this.Visit(expression.JoinCondition);
            if (!this.result)
            {
                return expression;
            }

            this.currentObject = current.Aliases;
            this.VisitObjectList(expression.Aliases);
            if (!this.result)
            {
                return expression;
            }

            this.currentObject = current;
            return expression;
        }

        protected override Expression VisitTable(SqlTableExpression expression)
        {
            SqlTableExpression current;
            if (!TryGetCurrent(expression, out current))
            {
                return expression;
            }

            if (!(this.result &= object.Equals(current.Name, expression.Name)))
            {
                return expression;
            }

            if (!(this.result &= current.NodeType == expression.NodeType))
            {
                return expression;
            }

            if (!(this.result &= object.Equals(current.Alias, expression.Alias)))
            {
                return expression;
            }

            if (!(this.result &= current.Type == expression.Type))
            {
                return expression;
            }

            this.currentObject = current.Aliases;
            this.VisitObjectList(expression.Aliases);
            if (!this.result)
            {
                return expression;
            }

            this.currentObject = current;
            return expression;
        }

        protected override Expression VisitColumn(SqlColumnExpression expression)
        {
            SqlColumnExpression current;
            if (!TryGetCurrent(expression, out current))
            {
                return expression;
            }

            if (!(this.result &= object.Equals(current.Name, expression.Name)))
            {
                return expression;
            }

            if (!(this.result &= current.Special == expression.Special))
            {
                return expression;
            }

            if (!(this.result &= object.Equals(current.SelectAlias, expression.SelectAlias)))
            {
                return expression;
            }

            if (!(this.result &= object.Equals(current.AliasedName, expression.AliasedName)))
            {
                return expression;
            }

            if (!(this.result &= current.NodeType == expression.NodeType))
            {
                return expression;
            }

            if (!(this.result &= current.Type == expression.Type))
            {
                return expression;
            }

            this.currentObject = current;
            return expression;
        }

        protected override Expression VisitFunctionCall(SqlFunctionCallExpression expression)
        {
            SqlFunctionCallExpression current;
            if (!TryGetCurrent(expression, out current))
            {
                return expression;
            }

            if (!(this.result &= current.Function == expression.Function))
            {
                return expression;
            }

            if (!(this.result &= object.Equals(current.UserDefinedFunctionName, expression.UserDefinedFunctionName)))
            {
                return expression;
            }

            if (!(this.result &= current.NodeType == expression.NodeType))
            {
                return expression;
            }

            if (!(this.result &= current.Type == expression.Type))
            {
                return expression;
            }

            this.currentObject = current.Arguments;
            this.VisitExpressionList(expression.Arguments);
            if (!this.result)
            {
                return expression;
            }

            this.currentObject = current;
            return expression;
        }

        protected override Expression VisitSubquery(SqlSubqueryExpression expression)
        {
            SqlSubqueryExpression current;
            if (!TryGetCurrent(expression, out current))
            {
                return expression;
            }

            if (!(this.result &= current.NodeType == expression.NodeType))
            {
                return expression;
            }

            if (!(this.result &= current.Type == expression.Type))
            {
                return expression;
            }

            this.currentObject = current.Select;
            this.VisitSelect(expression.Select);
            if (!this.result)
            {
                return expression;
            }

            this.currentObject = current;
            return expression;
        }

        protected override Expression VisitAggregate(SqlAggregateExpression expression)
        {
            SqlAggregateExpression current;
            if (!TryGetCurrent(expression, out current))
            {
                return expression;
            }

            if (!(this.result &= current.IsDistinct == expression.IsDistinct))
            {
                return expression;
            }

            if (!(this.result &= current.AggregateType == expression.AggregateType))
            {
                return expression;
            }

            if (!(this.result &= current.NodeType == expression.NodeType))
            {
                return expression;
            }

            if (!(this.result &= current.Type == expression.Type))
            {
                return expression;
            }

            this.currentObject = current.Argument;
            this.Visit(expression.Argument);
            if (!this.result)
            {
                return expression;
            }

            this.currentObject = current;
            return expression;
        }

        protected override Expression VisitAggregateSubquery(SqlAggregateSubqueryExpression expression)
        {
            SqlAggregateSubqueryExpression current;
            if (!TryGetCurrent(expression, out current))
            {
                return expression;
            }

            if (!(this.result &= object.Equals(current.GroupByAlias, expression.GroupByAlias)))
            {
                return expression;
            }

            if (!(this.result &= current.NodeType == expression.NodeType))
            {
                return expression;
            }

            if (!(this.result &= current.Type == expression.Type))
            {
                return expression;
            }

            this.currentObject = current.AggregateInGroupSelect;
            this.Visit(expression.AggregateInGroupSelect);
            if (!this.result)
            {
                return expression;
            }

            this.currentObject = current.AggregateAsSubquery;
            this.VisitSubquery(expression.AggregateAsSubquery);
            if (!this.result)
            {
                return expression;
            }

            this.currentObject = current;
            return expression;
        }

        protected override Expression VisitSelect(SqlSelectExpression expression)
        {
            SqlSelectExpression current;
            if (!TryGetCurrent(expression, out current))
            {
                return expression;
            }

            if (!(this.result &= current.Distinct == expression.Distinct))
            {
                return expression;
            }

            if (!(this.result &= current.ForUpdate == expression.ForUpdate))
            {
                return expression;
            }

            if (!(this.result &= current.Reverse == expression.Reverse))
            {
                return expression;
            }

            if (!(this.result &= current.NodeType == expression.NodeType))
            {
                return expression;
            }

            if (!(this.result &= object.Equals(current.Alias, expression.Alias)))
            {
                return expression;
            }

            if (!(this.result &= current.Type == expression.Type))
            {
                return expression;
            }

            this.currentObject = current.From;
            this.Visit(expression.From);
            if (!this.result)
            {
                return expression;
            }

            this.currentObject = current.Where;
            this.Visit(expression.Where);
            if (!this.result)
            {
                return expression;
            }

            this.currentObject = current.Take;
            this.Visit(expression.Take);
            if (!this.result)
            {
                return expression;
            }

            this.currentObject = current.Skip;
            this.Visit(expression.Skip);
            if (!this.result)
            {
                return expression;
            }

            this.currentObject = current.Into;
            this.Visit(expression.Into);
            if (!this.result)
            {
                return expression;
            }

            this.currentObject = current.OrderBy;
            this.VisitExpressionList(expression.OrderBy);
            if (!this.result)
            {
                return expression;
            }

            this.currentObject = current.GroupBy;
            this.VisitExpressionList(expression.GroupBy);
            if (!this.result)
            {
                return expression;
            }

            this.currentObject = current.Columns;
            this.VisitColumnDeclarations(expression.Columns);
            if (!this.result)
            {
                return expression;
            }

            this.currentObject = current.Aliases;
            this.VisitObjectList(expression.Aliases);
            if (!this.result)
            {
                return expression;
            }

            this.currentObject = current;
            return expression;
        }

        protected override Expression VisitOrderBy(SqlOrderByExpression expression)
        {
            SqlOrderByExpression current;
            if (!TryGetCurrent(expression, out current))
            {
                return expression;
            }

            if (!(this.result &= current.OrderType == expression.OrderType))
            {
                return expression;
            }

            if (!(this.result &= current.NodeType == expression.NodeType))
            {
                return expression;
            }

            if (!(this.result &= current.Type == expression.Type))
            {
                return expression;
            }

            this.currentObject = current.Expression;
            this.Visit(expression.Expression);
            if (!this.result)
            {
                return expression;
            }

            this.currentObject = current;
            return expression;
        }

        protected override Expression VisitProjection(SqlProjectionExpression expression)
        {
            SqlProjectionExpression current;
            if (!TryGetCurrent(expression, out current))
            {
                return expression;
            }

            if (!(this.result &= current.IsElementTableProjection == expression.IsElementTableProjection))
            {
                return expression;
            }

            if (!(this.result &= current.NodeType == expression.NodeType))
            {
                return expression;
            }

            if (!(this.result &= current.Type == expression.Type))
            {
                return expression;
            }

            this.currentObject = current.DefaultValue;
            this.Visit(expression.DefaultValue);
            if (!this.result)
            {
                return expression;
            }

            this.currentObject = current.Select;
            this.VisitSelect(expression.Select);
            if (!this.result)
            {
                return expression;
            }

            this.currentObject = current.Projector;
            this.Visit(expression.Projector);
            if (!this.result)
            {
                return expression;
            }

            this.currentObject = current.Aggregator;
            this.VisitLambda(expression.Aggregator);
            if (!this.result)
            {
                return expression;
            }

            this.currentObject = current;
            return expression;
        }

        protected override SqlColumnDeclaration VisitColumnDeclaration(SqlColumnDeclaration expression)
        {
            SqlColumnDeclaration current;
            if (!TryGetCurrent(expression, out current))
            {
                return expression;
            }

            if (!(this.result &= object.Equals(current.Name, expression.Name)))
            {
                return expression;
            }

            if (!(this.result &= current.NoOptimise == expression.NoOptimise))
            {
                return expression;
            }

            this.currentObject = current.Expression;
            this.Visit(expression.Expression);
            if (!this.result)
            {
                return expression;
            }

            this.currentObject = current;
            return expression;
        }

        protected override Expression VisitDelete(SqlDeleteExpression expression)
        {
            SqlDeleteExpression current;
            if (!TryGetCurrent(expression, out current))
            {
                return expression;
            }

            if (!(this.result &= current.NodeType == expression.NodeType))
            {
                return expression;
            }

            if (!(this.result &= current.Type == expression.Type))
            {
                return expression;
            }

            this.currentObject = current.Source;
            this.Visit(expression.Source);
            if (!this.result)
            {
                return expression;
            }

            this.currentObject = current.Where;
            this.Visit(expression.Where);
            if (!this.result)
            {
                return expression;
            }

            this.currentObject = current;
            return expression;
        }

        protected override Expression VisitColumnDefinition(SqlColumnDefinitionExpression expression)
        {
            SqlColumnDefinitionExpression current;
            if (!TryGetCurrent(expression, out current))
            {
                return expression;
            }

            if (!(this.result &= object.Equals(current.ColumnName, expression.ColumnName)))
            {
                return expression;
            }

            if (!(this.result &= current.NodeType == expression.NodeType))
            {
                return expression;
            }

            if (!(this.result &= current.Type == expression.Type))
            {
                return expression;
            }

            this.currentObject = current.ColumnType;
            this.Visit(expression.ColumnType);
            if (!this.result)
            {
                return expression;
            }

            this.currentObject = current.ConstraintExpressions;
            this.VisitExpressionList(expression.ConstraintExpressions);
            if (!this.result)
            {
                return expression;
            }

            this.currentObject = current;
            return expression;
        }

        protected override Expression VisitCreateTable(SqlCreateTableExpression expression)
        {
            SqlCreateTableExpression current;
            if (!TryGetCurrent(expression, out current))
            {
                return expression;
            }

            if (!(this.result &= current.IfNotExist == expression.IfNotExist))
            {
                return expression;
            }

            if (!(this.result &= current.NodeType == expression.NodeType))
            {
                return expression;
            }

            if (!(this.result &= current.Type == expression.Type))
            {
                return expression;
            }

            this.currentObject = current.Table;
            this.VisitTable(expression.Table);
            if (!this.result)
            {
                return expression;
            }

            this.currentObject = current.TableOptions;
            this.VisitObjectList(expression.TableOptions);
            if (!this.result)
            {
                return expression;
            }

            this.currentObject = current.TableConstraints;
            this.VisitExpressionList(expression.TableConstraints);
            if (!this.result)
            {
                return expression;
            }

            this.currentObject = current.ColumnDefinitionExpressions;
            this.VisitExpressionList(expression.ColumnDefinitionExpressions);
            if (!this.result)
            {
                return expression;
            }

            this.currentObject = current;
            return expression;
        }

        protected override Expression VisitAlterTable(SqlAlterTableExpression expression)
        {
            SqlAlterTableExpression current;
            if (!TryGetCurrent(expression, out current))
            {
                return expression;
            }

            if (!(this.result &= current.NodeType == expression.NodeType))
            {
                return expression;
            }

            if (!(this.result &= current.Type == expression.Type))
            {
                return expression;
            }

            this.currentObject = current.Table;
            this.Visit(expression.Table);
            if (!this.result)
            {
                return expression;
            }

            this.currentObject = current.Actions;
            this.VisitExpressionList(expression.Actions);
            if (!this.result)
            {
                return expression;
            }

            this.currentObject = current.ConstraintActions;
            this.VisitExpressionList(expression.ConstraintActions);
            if (!this.result)
            {
                return expression;
            }

            this.currentObject = current;
            return expression;
        }

        protected override Expression VisitConstraintAction(SqlConstraintActionExpression expression)
        {
            SqlConstraintActionExpression current;
            if (!TryGetCurrent(expression, out current))
            {
                return expression;
            }

            if (!(this.result &= current.ActionType == expression.ActionType))
            {
                return expression;
            }

            if (!(this.result &= current.NodeType == expression.NodeType))
            {
                return expression;
            }

            if (!(this.result &= current.Type == expression.Type))
            {
                return expression;
            }

            this.currentObject = current.ConstraintExpression;
            this.Visit(expression.ConstraintExpression);
            if (!this.result)
            {
                return expression;
            }

            this.currentObject = current;
            return expression;
        }

        protected override Expression VisitCreateIndex(SqlCreateIndexExpression expression)
        {
            SqlCreateIndexExpression current;
            if (!TryGetCurrent(expression, out current))
            {
                return expression;
            }

            if (!(this.result &= current.Unique == expression.Unique))
            {
                return expression;
            }

            if (!(this.result &= object.Equals(current.IndexName, expression.IndexName)))
            {
                return expression;
            }

            if (!(this.result &= current.IfNotExist == expression.IfNotExist))
            {
                return expression;
            }

            if (!(this.result &= current.LowercaseIndex == expression.LowercaseIndex))
            {
                return expression;
            }

            if (!(this.result &= current.IndexType == expression.IndexType))
            {
                return expression;
            }

            if (!(this.result &= current.NodeType == expression.NodeType))
            {
                return expression;
            }

            if (!(this.result &= current.Type == expression.Type))
            {
                return expression;
            }

            this.currentObject = current.Table;
            this.VisitTable(expression.Table);
            if (!this.result)
            {
                return expression;
            }

            this.currentObject = current.Where;
            this.Visit(expression.Where);
            if (!this.result)
            {
                return expression;
            }

            this.currentObject = current.Columns;
            this.VisitExpressionList(expression.Columns);
            if (!this.result)
            {
                return expression;
            }

            this.currentObject = current.IncludedColumns;
            this.VisitExpressionList(expression.IncludedColumns);
            if (!this.result)
            {
                return expression;
            }

            this.currentObject = current;
            return expression;
        }

        protected override Expression VisitStatementList(SqlStatementListExpression expression)
        {
            SqlStatementListExpression current;
            if (!TryGetCurrent(expression, out current))
            {
                return expression;
            }

            if (!(this.result &= current.NodeType == expression.NodeType))
            {
                return expression;
            }

            if (!(this.result &= current.Type == expression.Type))
            {
                return expression;
            }

            this.currentObject = current.Statements;
            this.VisitExpressionList(expression.Statements);
            if (!this.result)
            {
                return expression;
            }

            this.currentObject = current;
            return expression;
        }

        protected override Expression VisitConstraint(SqlConstraintExpression expression)
        {
            SqlConstraintExpression current;
            if (!TryGetCurrent(expression, out current))
            {
                return expression;
            }

            if (!(this.result &= object.Equals(current.ConstraintName, expression.ConstraintName)))
            {
                return expression;
            }

            if (!(this.result &= current.SimpleConstraint == expression.SimpleConstraint))
            {
                return expression;
            }

            if (!(this.result &= current.NodeType == expression.NodeType))
            {
                return expression;
            }

            if (!(this.result &= current.Type == expression.Type))
            {
                return expression;
            }

            this.currentObject = current.ReferencesExpression;
            this.VisitReferences(expression.ReferencesExpression);
            if (!this.result)
            {
                return expression;
            }

            this.currentObject = current.DefaultValue;
            this.Visit(expression.DefaultValue);
            if (!this.result)
            {
                return expression;
            }

            this.currentObject = current.ColumnNames;
            this.VisitObjectList(expression.ColumnNames);
            if (!this.result)
            {
                return expression;
            }

            this.currentObject = current.ConstraintOptions;
            this.VisitObjectList(expression.ConstraintOptions);
            if (!this.result)
            {
                return expression;
            }

            this.currentObject = current;
            return expression;
        }

        protected override Expression VisitIndexedColumn(SqlIndexedColumnExpression expression)
        {
            SqlIndexedColumnExpression current;
            if (!TryGetCurrent(expression, out current))
            {
                return expression;
            }

            if (!(this.result &= current.LowercaseIndex == expression.LowercaseIndex))
            {
                return expression;
            }

            if (!(this.result &= current.SortOrder == expression.SortOrder))
            {
                return expression;
            }

            if (!(this.result &= current.NodeType == expression.NodeType))
            {
                return expression;
            }

            if (!(this.result &= current.Type == expression.Type))
            {
                return expression;
            }

            this.currentObject = current.Column;
            this.VisitColumn(expression.Column);
            if (!this.result)
            {
                return expression;
            }

            this.currentObject = current;
            return expression;
        }

        protected override Expression VisitQueryArgument(SqlQueryArgumentExpression expression)
        {
            SqlQueryArgumentExpression current;
            if (!TryGetCurrent(expression, out current))
            {
                return expression;
            }

            if (!(this.result &= current.Index == expression.Index))
            {
                return expression;
            }

            if (!(this.result &= current.NodeType == expression.NodeType))
            {
                return expression;
            }

            if (!(this.result &= current.Type == expression.Type))
            {
                return expression;
            }

            this.currentObject = current;
            return expression;
        }

        protected override Expression VisitUnion(SqlUnionExpression expression)
        {
            SqlUnionExpression current;
            if (!TryGetCurrent(expression, out current))
            {
                return expression;
            }

            if (!(this.result &= current.UnionAll == expression.UnionAll))
            {
                return expression;
            }

            if (!(this.result &= current.NodeType == expression.NodeType))
            {
                return expression;
            }

            if (!(this.result &= object.Equals(current.Alias, expression.Alias)))
            {
                return expression;
            }

            if (!(this.result &= current.Type == expression.Type))
            {
                return expression;
            }

            this.currentObject = current.Left;
            this.Visit(expression.Left);
            if (!this.result)
            {
                return expression;
            }

            this.currentObject = current.Right;
            this.Visit(expression.Right);
            if (!this.result)
            {
                return expression;
            }

            this.currentObject = current.Aliases;
            this.VisitObjectList(expression.Aliases);
            if (!this.result)
            {
                return expression;
            }

            this.currentObject = current;
            return expression;
        }

        protected override Expression VisitLabel(LabelExpression expression)
        {
            LabelExpression current;
            if (!TryGetCurrent(expression, out current))
            {
                return expression;
            }

            if (!(this.result &= current.Type == expression.Type))
            {
                return expression;
            }

            if (!(this.result &= current.NodeType == expression.NodeType))
            {
                return expression;
            }

            this.currentObject = current.Target;
            this.VisitLabelTarget(expression.Target);
            if (!this.result)
            {
                return expression;
            }

            this.currentObject = current.DefaultValue;
            this.Visit(expression.DefaultValue);
            if (!this.result)
            {
                return expression;
            }

            this.currentObject = current;
            return expression;
        }

        protected override Expression VisitGoto(GotoExpression expression)
        {
            GotoExpression current;
            if (!TryGetCurrent(expression, out current))
            {
                return expression;
            }

            if (!(this.result &= current.Type == expression.Type))
            {
                return expression;
            }

            if (!(this.result &= current.NodeType == expression.NodeType))
            {
                return expression;
            }

            if (!(this.result &= current.Kind == expression.Kind))
            {
                return expression;
            }

            this.currentObject = current.Value;
            this.Visit(expression.Value);
            if (!this.result)
            {
                return expression;
            }

            this.currentObject = current.Target;
            this.VisitLabelTarget(expression.Target);
            if (!this.result)
            {
                return expression;
            }

            this.currentObject = current;
            return expression;
        }

        protected override Expression VisitBlock(BlockExpression expression)
        {
            BlockExpression current;
            if (!TryGetCurrent(expression, out current))
            {
                return expression;
            }

            if (!(this.result &= current.NodeType == expression.NodeType))
            {
                return expression;
            }

            if (!(this.result &= current.Type == expression.Type))
            {
                return expression;
            }

            this.currentObject = current.Result;
            this.Visit(expression.Result);
            if (!this.result)
            {
                return expression;
            }

            this.currentObject = current.Expressions;
            this.VisitExpressionList(expression.Expressions);
            if (!this.result)
            {
                return expression;
            }

            this.currentObject = current.Variables;
            this.VisitExpressionList(expression.Variables);
            if (!this.result)
            {
                return expression;
            }

            this.currentObject = current;
            return expression;
        }

        protected override MemberBinding VisitBinding(MemberBinding expression)
        {
            MemberBinding current;
            if (!TryGetCurrent(expression, out current))
            {
                return expression;
            }

            if (!(this.result &= current.BindingType == expression.BindingType))
            {
                return expression;
            }

            if (!(this.result &= object.Equals(current.Member, expression.Member)))
            {
                return expression;
            }

            this.currentObject = current;
            return expression;
        }

        protected override ElementInit VisitElementInitializer(ElementInit expression)
        {
            ElementInit current;
            if (!TryGetCurrent(expression, out current))
            {
                return expression;
            }

            if (!(this.result &= current.AddMethod == expression.AddMethod))
            {
                return expression;
            }

            this.currentObject = current.Arguments;
            this.VisitExpressionList(expression.Arguments);
            if (!this.result)
            {
                return expression;
            }

            this.currentObject = current;
            return expression;
        }

        protected override Expression VisitUnary(UnaryExpression expression)
        {
            UnaryExpression current;
            if (!TryGetCurrent(expression, out current))
            {
                return expression;
            }

            if (!(this.result &= current.Type == expression.Type))
            {
                return expression;
            }

            if (!(this.result &= current.NodeType == expression.NodeType))
            {
                return expression;
            }

            if (!(this.result &= current.Method == expression.Method))
            {
                return expression;
            }

            if (!(this.result &= current.IsLifted == expression.IsLifted))
            {
                return expression;
            }

            if (!(this.result &= current.IsLiftedToNull == expression.IsLiftedToNull))
            {
                return expression;
            }

            this.currentObject = current.Operand;
            this.Visit(expression.Operand);
            if (!this.result)
            {
                return expression;
            }

            this.currentObject = current;
            return expression;
        }

        protected override Expression VisitBinary(BinaryExpression expression)
        {
            BinaryExpression current;
            if (!TryGetCurrent(expression, out current))
            {
                return expression;
            }

            if (!(this.result &= current.Method == expression.Method))
            {
                return expression;
            }

            if (!(this.result &= current.IsLifted == expression.IsLifted))
            {
                return expression;
            }

            if (!(this.result &= current.IsLiftedToNull == expression.IsLiftedToNull))
            {
                return expression;
            }

            if (!(this.result &= current.NodeType == expression.NodeType))
            {
                return expression;
            }

            if (!(this.result &= current.Type == expression.Type))
            {
                return expression;
            }

            this.currentObject = current.Right;
            this.Visit(expression.Right);
            if (!this.result)
            {
                return expression;
            }

            this.currentObject = current.Left;
            this.Visit(expression.Left);
            if (!this.result)
            {
                return expression;
            }

            this.currentObject = current.Conversion;
            this.VisitLambda(expression.Conversion);
            if (!this.result)
            {
                return expression;
            }

            this.currentObject = current;
            return expression;
        }

        protected override Expression VisitTypeIs(TypeBinaryExpression expression)
        {
            TypeBinaryExpression current;
            if (!TryGetCurrent(expression, out current))
            {
                return expression;
            }

            if (!(this.result &= current.Type == expression.Type))
            {
                return expression;
            }

            if (!(this.result &= current.NodeType == expression.NodeType))
            {
                return expression;
            }

            if (!(this.result &= current.TypeOperand == expression.TypeOperand))
            {
                return expression;
            }

            this.currentObject = current.Expression;
            this.Visit(expression.Expression);
            if (!this.result)
            {
                return expression;
            }

            this.currentObject = current;
            return expression;
        }

        protected override Expression VisitConditional(ConditionalExpression expression)
        {
            ConditionalExpression current;
            if (!TryGetCurrent(expression, out current))
            {
                return expression;
            }

            if (!(this.result &= current.NodeType == expression.NodeType))
            {
                return expression;
            }

            if (!(this.result &= current.Type == expression.Type))
            {
                return expression;
            }

            this.currentObject = current.Test;
            this.Visit(expression.Test);
            if (!this.result)
            {
                return expression;
            }

            this.currentObject = current.IfTrue;
            this.Visit(expression.IfTrue);
            if (!this.result)
            {
                return expression;
            }

            this.currentObject = current.IfFalse;
            this.Visit(expression.IfFalse);
            if (!this.result)
            {
                return expression;
            }

            this.currentObject = current;
            return expression;
        }

        protected override Expression VisitParameter(ParameterExpression expression)
        {
            ParameterExpression current;
            if (!TryGetCurrent(expression, out current))
            {
                return expression;
            }

            if (!(this.result &= current.Type == expression.Type))
            {
                return expression;
            }

            if (!(this.result &= current.NodeType == expression.NodeType))
            {
                return expression;
            }

            if (!(this.result &= object.Equals(current.Name, expression.Name)))
            {
                return expression;
            }

            if (!(this.result &= current.IsByRef == expression.IsByRef))
            {
                return expression;
            }

            this.currentObject = current;
            return expression;
        }

        protected override Expression VisitMemberAccess(MemberExpression expression)
        {
            MemberExpression current;
            if (!TryGetCurrent(expression, out current))
            {
                return expression;
            }

            if (!(this.result &= object.Equals(current.Member, expression.Member)))
            {
                return expression;
            }

            if (!(this.result &= current.NodeType == expression.NodeType))
            {
                return expression;
            }

            if (!(this.result &= current.Type == expression.Type))
            {
                return expression;
            }

            this.currentObject = current.Expression;
            this.Visit(expression.Expression);
            if (!this.result)
            {
                return expression;
            }

            this.currentObject = current;
            return expression;
        }

        protected override Expression VisitMethodCall(MethodCallExpression expression)
        {
            MethodCallExpression current;
            if (!TryGetCurrent(expression, out current))
            {
                return expression;
            }

            if (!(this.result &= current.NodeType == expression.NodeType))
            {
                return expression;
            }

            if (!(this.result &= current.Type == expression.Type))
            {
                return expression;
            }

            if (!(this.result &= current.Method == expression.Method))
            {
                return expression;
            }

            this.currentObject = current.Object;
            this.Visit(expression.Object);
            if (!this.result)
            {
                return expression;
            }

            this.currentObject = current.Arguments;
            this.VisitExpressionList(expression.Arguments);
            if (!this.result)
            {
                return expression;
            }

            this.currentObject = current;
            return expression;
        }

        protected override MemberAssignment VisitMemberAssignment(MemberAssignment expression)
        {
            MemberAssignment current;
            if (!TryGetCurrent(expression, out current))
            {
                return expression;
            }

            if (!(this.result &= current.BindingType == expression.BindingType))
            {
                return expression;
            }

            if (!(this.result &= object.Equals(current.Member, expression.Member)))
            {
                return expression;
            }

            this.currentObject = current.Expression;
            this.Visit(expression.Expression);
            if (!this.result)
            {
                return expression;
            }

            this.currentObject = current;
            return expression;
        }

        protected override MemberMemberBinding VisitMemberMemberBinding(MemberMemberBinding expression)
        {
            MemberMemberBinding current;
            if (!TryGetCurrent(expression, out current))
            {
                return expression;
            }

            if (!(this.result &= current.BindingType == expression.BindingType))
            {
                return expression;
            }

            if (!(this.result &= object.Equals(current.Member, expression.Member)))
            {
                return expression;
            }

            this.currentObject = current.Bindings;
            this.VisitBindingList(expression.Bindings);
            if (!this.result)
            {
                return expression;
            }

            this.currentObject = current;
            return expression;
        }

        protected override MemberListBinding VisitMemberListBinding(MemberListBinding expression)
        {
            MemberListBinding current;
            if (!TryGetCurrent(expression, out current))
            {
                return expression;
            }

            if (!(this.result &= current.BindingType == expression.BindingType))
            {
                return expression;
            }

            if (!(this.result &= object.Equals(current.Member, expression.Member)))
            {
                return expression;
            }

            this.currentObject = current.Initializers;
            this.VisitElementInitializerList(expression.Initializers);
            if (!this.result)
            {
                return expression;
            }

            this.currentObject = current;
            return expression;
        }

        protected override Expression VisitLambda(LambdaExpression expression)
        {
            LambdaExpression current;
            if (!TryGetCurrent(expression, out current))
            {
                return expression;
            }

            if (!(this.result &= current.Type == expression.Type))
            {
                return expression;
            }

            if (!(this.result &= current.NodeType == expression.NodeType))
            {
                return expression;
            }

            if (!(this.result &= object.Equals(current.Name, expression.Name)))
            {
                return expression;
            }

            if (!(this.result &= current.ReturnType == expression.ReturnType))
            {
                return expression;
            }

            if (!(this.result &= current.TailCall == expression.TailCall))
            {
                return expression;
            }

            this.currentObject = current.Body;
            this.Visit(expression.Body);
            if (!this.result)
            {
                return expression;
            }

            this.currentObject = current.Parameters;
            this.VisitExpressionList(expression.Parameters);
            if (!this.result)
            {
                return expression;
            }

            this.currentObject = current;
            return expression;
        }

        protected override Expression VisitNew(NewExpression expression)
        {
            NewExpression current;
            if (!TryGetCurrent(expression, out current))
            {
                return expression;
            }

            if (!(this.result &= current.Type == expression.Type))
            {
                return expression;
            }

            if (!(this.result &= current.NodeType == expression.NodeType))
            {
                return expression;
            }

            if (!(this.result &= object.Equals(current.Constructor, expression.Constructor)))
            {
                return expression;
            }

            this.currentObject = current.Arguments;
            this.VisitExpressionList(expression.Arguments);
            if (!this.result)
            {
                return expression;
            }

            this.currentObject = current.Members;
            this.VisitObjectList(expression.Members);
            if (!this.result)
            {
                return expression;
            }

            this.currentObject = current;
            return expression;
        }

        protected override Expression VisitMemberInit(MemberInitExpression expression)
        {
            MemberInitExpression current;
            if (!TryGetCurrent(expression, out current))
            {
                return expression;
            }

            if (!(this.result &= current.Type == expression.Type))
            {
                return expression;
            }

            if (!(this.result &= current.NodeType == expression.NodeType))
            {
                return expression;
            }

            this.currentObject = current.NewExpression;
            this.VisitNew(expression.NewExpression);
            if (!this.result)
            {
                return expression;
            }

            this.currentObject = current.Bindings;
            this.VisitBindingList(expression.Bindings);
            if (!this.result)
            {
                return expression;
            }

            this.currentObject = current;
            return expression;
        }

        protected override Expression VisitListInit(ListInitExpression expression)
        {
            ListInitExpression current;
            if (!TryGetCurrent(expression, out current))
            {
                return expression;
            }

            if (!(this.result &= current.NodeType == expression.NodeType))
            {
                return expression;
            }

            if (!(this.result &= current.Type == expression.Type))
            {
                return expression;
            }

            this.currentObject = current.NewExpression;
            this.VisitNew(expression.NewExpression);
            if (!this.result)
            {
                return expression;
            }

            this.currentObject = current.Initializers;
            this.VisitElementInitializerList(expression.Initializers);
            if (!this.result)
            {
                return expression;
            }

            this.currentObject = current;
            return expression;
        }

        protected override Expression VisitNewArray(NewArrayExpression expression)
        {
            NewArrayExpression current;
            if (!TryGetCurrent(expression, out current))
            {
                return expression;
            }

            if (!(this.result &= current.Type == expression.Type))
            {
                return expression;
            }

            if (!(this.result &= current.NodeType == expression.NodeType))
            {
                return expression;
            }

            this.currentObject = current.Expressions;
            this.VisitExpressionList(expression.Expressions);
            if (!this.result)
            {
                return expression;
            }

            this.currentObject = current;
            return expression;
        }

        protected override Expression VisitInvocation(InvocationExpression expression)
        {
            InvocationExpression current;
            if (!TryGetCurrent(expression, out current))
            {
                return expression;
            }

            if (!(this.result &= current.Type == expression.Type))
            {
                return expression;
            }

            if (!(this.result &= current.NodeType == expression.NodeType))
            {
                return expression;
            }

            this.currentObject = current.Expression;
            this.Visit(expression.Expression);
            if (!this.result)
            {
                return expression;
            }

            this.currentObject = current.Arguments;
            this.VisitExpressionList(expression.Arguments);
            if (!this.result)
            {
                return expression;
            }

            this.currentObject = current;
            return expression;
        }
    }
}