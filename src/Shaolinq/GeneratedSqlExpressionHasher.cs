#pragma warning disable
using System.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Expressions
{
    public partial class SqlExpressionHasher
    {
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

        protected override Expression VisitReferencesColumn(SqlReferencesColumnExpression expression)
        {
            this.hashCode ^= expression.Deferrability.GetHashCode();
            this.hashCode ^= expression.OnDeleteAction.GetHashCode();
            this.hashCode ^= expression.OnUpdateAction.GetHashCode();
            return base.VisitReferencesColumn(expression);
        }

        protected override Expression VisitSimpleConstraint(SqlSimpleConstraintExpression expression)
        {
            this.hashCode ^= expression.Value?.GetHashCode() ?? 0;
            this.hashCode ^= expression.Constraint.GetHashCode();
            return base.VisitSimpleConstraint(expression);
        }

        protected override Expression VisitForeignKeyConstraint(SqlForeignKeyConstraintExpression expression)
        {
            this.hashCode ^= expression.ConstraintName?.GetHashCode() ?? 0;
            return base.VisitForeignKeyConstraint(expression);
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

        protected override Expression VisitLabel(LabelExpression expression)
        {
            this.hashCode ^= expression.Target?.GetHashCode() ?? 0;
            return base.VisitLabel(expression);
        }

        protected override Expression VisitGoto(GotoExpression expression)
        {
            this.hashCode ^= expression.Target?.GetHashCode() ?? 0;
            this.hashCode ^= expression.Kind.GetHashCode();
            return base.VisitGoto(expression);
        }

        protected override MemberBinding VisitBinding(MemberBinding expression)
        {
            this.hashCode ^= expression.BindingType.GetHashCode();
            this.hashCode ^= expression.Member?.GetHashCode() ?? 0;
            return base.VisitBinding(expression);
        }

        protected override ElementInit VisitElementInitializer(ElementInit expression)
        {
            this.hashCode ^= expression.AddMethod?.GetHashCode() ?? 0;
            return base.VisitElementInitializer(expression);
        }

        protected override Expression VisitUnary(UnaryExpression expression)
        {
            this.hashCode ^= expression.Method?.GetHashCode() ?? 0;
            this.hashCode ^= expression.IsLifted ? 961965302 : 0;
            this.hashCode ^= expression.IsLiftedToNull ? -692903591 : 0;
            return base.VisitUnary(expression);
        }

        protected override Expression VisitBinary(BinaryExpression expression)
        {
            this.hashCode ^= expression.Method?.GetHashCode() ?? 0;
            this.hashCode ^= expression.IsLifted ? 961965302 : 0;
            this.hashCode ^= expression.IsLiftedToNull ? -692903591 : 0;
            return base.VisitBinary(expression);
        }

        protected override Expression VisitTypeIs(TypeBinaryExpression expression)
        {
            this.hashCode ^= expression.TypeOperand?.GetHashCode() ?? 0;
            return base.VisitTypeIs(expression);
        }

        protected override Expression VisitParameter(ParameterExpression expression)
        {
            this.hashCode ^= expression.Name?.GetHashCode() ?? 0;
            this.hashCode ^= expression.IsByRef ? -1802356633 : 0;
            return base.VisitParameter(expression);
        }

        protected override Expression VisitMemberAccess(MemberExpression expression)
        {
            this.hashCode ^= expression.Member?.GetHashCode() ?? 0;
            return base.VisitMemberAccess(expression);
        }

        protected override Expression VisitMethodCall(MethodCallExpression expression)
        {
            this.hashCode ^= expression.Method?.GetHashCode() ?? 0;
            return base.VisitMethodCall(expression);
        }

        protected override MemberAssignment VisitMemberAssignment(MemberAssignment expression)
        {
            this.hashCode ^= expression.BindingType.GetHashCode();
            this.hashCode ^= expression.Member?.GetHashCode() ?? 0;
            return base.VisitMemberAssignment(expression);
        }

        protected override MemberMemberBinding VisitMemberMemberBinding(MemberMemberBinding expression)
        {
            this.hashCode ^= expression.BindingType.GetHashCode();
            this.hashCode ^= expression.Member?.GetHashCode() ?? 0;
            return base.VisitMemberMemberBinding(expression);
        }

        protected override MemberListBinding VisitMemberListBinding(MemberListBinding expression)
        {
            this.hashCode ^= expression.BindingType.GetHashCode();
            this.hashCode ^= expression.Member?.GetHashCode() ?? 0;
            return base.VisitMemberListBinding(expression);
        }

        protected override Expression VisitLambda(LambdaExpression expression)
        {
            this.hashCode ^= expression.Name?.GetHashCode() ?? 0;
            this.hashCode ^= expression.ReturnType?.GetHashCode() ?? 0;
            this.hashCode ^= expression.TailCall ? 1121030802 : 0;
            return base.VisitLambda(expression);
        }

        protected override Expression VisitNew(NewExpression expression)
        {
            this.hashCode ^= expression.Constructor?.GetHashCode() ?? 0;
            return base.VisitNew(expression);
        }
    }
}