#pragma warning disable
using System.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Expressions
{
    public partial class SqlExpressionHasher
    {
        protected override Expression VisitReferences(SqlReferencesExpression expression)
        {
            this.hashCode ^= expression.Deferrability.GetHashCode();
            this.hashCode ^= expression.OnDeleteAction.GetHashCode();
            this.hashCode ^= expression.OnUpdateAction.GetHashCode();
            return base.VisitReferences(expression);
        }

        protected override Expression VisitVariableDeclaration(SqlVariableDeclarationExpression expression)
        {
            this.hashCode ^= expression.Name?.GetHashCode() ?? 0;
            return base.VisitVariableDeclaration(expression);
        }

        protected override Expression VisitKeyword(SqlKeywordExpression expression)
        {
            this.hashCode ^= expression.Name?.GetHashCode() ?? 0;
            return base.VisitKeyword(expression);
        }

        protected override Expression VisitTableHint(SqlTableHintExpression expression)
        {
            this.hashCode ^= expression.TableLock ? 233222230 : 0;
            return base.VisitTableHint(expression);
        }

        protected override Expression VisitSetCommand(SqlSetCommandExpression expression)
        {
            this.hashCode ^= expression.ConfigurationParameter?.GetHashCode() ?? 0;
            return base.VisitSetCommand(expression);
        }

        protected override Expression VisitPragma(SqlPragmaExpression expression)
        {
            this.hashCode ^= expression.Directive?.GetHashCode() ?? 0;
            return base.VisitPragma(expression);
        }

        protected override Expression VisitType(SqlTypeExpression expression)
        {
            this.hashCode ^= expression.TypeName?.GetHashCode() ?? 0;
            this.hashCode ^= expression.UserDefinedType ? -1063728409 : 0;
            return base.VisitType(expression);
        }

        protected override Expression VisitCreateType(SqlCreateTypeExpression expression)
        {
            this.hashCode ^= expression.IfNotExist ? 1286760945 : 0;
            return base.VisitCreateType(expression);
        }

        protected override Expression VisitUpdate(SqlUpdateExpression expression)
        {
            this.hashCode ^= expression.RequiresIdentityInsert ? 2044950846 : 0;
            return base.VisitUpdate(expression);
        }

        protected override Expression VisitInsertInto(SqlInsertIntoExpression expression)
        {
            this.hashCode ^= expression.RequiresIdentityInsert ? 2044950846 : 0;
            return base.VisitInsertInto(expression);
        }

        protected override Expression VisitJoin(SqlJoinExpression expression)
        {
            this.hashCode ^= expression.JoinType.GetHashCode();
            return base.VisitJoin(expression);
        }

        protected override Expression VisitTable(SqlTableExpression expression)
        {
            this.hashCode ^= expression.Name?.GetHashCode() ?? 0;
            this.hashCode ^= expression.Alias?.GetHashCode() ?? 0;
            return base.VisitTable(expression);
        }

        protected override Expression VisitColumn(SqlColumnExpression expression)
        {
            this.hashCode ^= expression.Name?.GetHashCode() ?? 0;
            this.hashCode ^= expression.Special ? 237046631 : 0;
            this.hashCode ^= expression.SelectAlias?.GetHashCode() ?? 0;
            this.hashCode ^= expression.AliasedName?.GetHashCode() ?? 0;
            return base.VisitColumn(expression);
        }

        protected override Expression VisitFunctionCall(SqlFunctionCallExpression expression)
        {
            this.hashCode ^= expression.Function.GetHashCode();
            this.hashCode ^= expression.UserDefinedFunctionName?.GetHashCode() ?? 0;
            return base.VisitFunctionCall(expression);
        }

        protected override Expression VisitAggregate(SqlAggregateExpression expression)
        {
            this.hashCode ^= expression.IsDistinct ? 1649375286 : 0;
            this.hashCode ^= expression.AggregateType.GetHashCode();
            return base.VisitAggregate(expression);
        }

        protected override Expression VisitAggregateSubquery(SqlAggregateSubqueryExpression expression)
        {
            this.hashCode ^= expression.GroupByAlias?.GetHashCode() ?? 0;
            return base.VisitAggregateSubquery(expression);
        }

        protected override Expression VisitSelect(SqlSelectExpression expression)
        {
            this.hashCode ^= expression.Distinct ? 309065085 : 0;
            this.hashCode ^= expression.ForUpdate ? -1548991485 : 0;
            this.hashCode ^= expression.Reverse ? 1018104131 : 0;
            this.hashCode ^= expression.Alias?.GetHashCode() ?? 0;
            return base.VisitSelect(expression);
        }

        protected override Expression VisitOrderBy(SqlOrderByExpression expression)
        {
            this.hashCode ^= expression.OrderType.GetHashCode();
            return base.VisitOrderBy(expression);
        }

        protected override Expression VisitProjection(SqlProjectionExpression expression)
        {
            this.hashCode ^= expression.IsElementTableProjection ? -751039749 : 0;
            return base.VisitProjection(expression);
        }

        protected override SqlColumnDeclaration VisitColumnDeclaration(SqlColumnDeclaration expression)
        {
            this.hashCode ^= expression.Name?.GetHashCode() ?? 0;
            this.hashCode ^= expression.NoOptimise ? 398538026 : 0;
            return base.VisitColumnDeclaration(expression);
        }

        protected override Expression VisitColumnDefinition(SqlColumnDefinitionExpression expression)
        {
            this.hashCode ^= expression.ColumnName?.GetHashCode() ?? 0;
            return base.VisitColumnDefinition(expression);
        }

        protected override Expression VisitCreateTable(SqlCreateTableExpression expression)
        {
            this.hashCode ^= expression.IfNotExist ? 1286760945 : 0;
            return base.VisitCreateTable(expression);
        }

        protected override Expression VisitConstraintAction(SqlConstraintActionExpression expression)
        {
            this.hashCode ^= expression.ActionType.GetHashCode();
            return base.VisitConstraintAction(expression);
        }

        protected override Expression VisitCreateIndex(SqlCreateIndexExpression expression)
        {
            this.hashCode ^= expression.Unique ? 379717795 : 0;
            this.hashCode ^= expression.IndexName?.GetHashCode() ?? 0;
            this.hashCode ^= expression.IfNotExist ? 1286760945 : 0;
            this.hashCode ^= expression.LowercaseIndex ? -692148566 : 0;
            this.hashCode ^= expression.IndexType.GetHashCode();
            return base.VisitCreateIndex(expression);
        }

        protected override Expression VisitConstraint(SqlConstraintExpression expression)
        {
            this.hashCode ^= expression.ConstraintName?.GetHashCode() ?? 0;
            this.hashCode ^= expression.ConstraintType.GetHashCode();
            this.hashCode ^= expression.NotNull ? 539833539 : 0;
            this.hashCode ^= expression.AutoIncrement ? 1811025211 : 0;
            this.hashCode ^= expression.Unique ? 379717795 : 0;
            this.hashCode ^= expression.PrimaryKey ? -1200056272 : 0;
            return base.VisitConstraint(expression);
        }

        protected override Expression VisitIndexedColumn(SqlIndexedColumnExpression expression)
        {
            this.hashCode ^= expression.LowercaseIndex ? -692148566 : 0;
            this.hashCode ^= expression.SortOrder.GetHashCode();
            return base.VisitIndexedColumn(expression);
        }

        protected override Expression VisitQueryArgument(SqlQueryArgumentExpression expression)
        {
            this.hashCode ^= expression.Index.GetHashCode();
            return base.VisitQueryArgument(expression);
        }

        protected override Expression VisitUnion(SqlUnionExpression expression)
        {
            this.hashCode ^= expression.UnionAll ? 1234145998 : 0;
            this.hashCode ^= expression.Alias?.GetHashCode() ?? 0;
            return base.VisitUnion(expression);
        }
    }
}