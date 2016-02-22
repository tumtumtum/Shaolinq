// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System.Linq.Expressions;
using Shaolinq.Persistence.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Optimizers
{
    public class AliasReferenceReplacer
        : SqlExpressionVisitor
    {
        private readonly string alias;
        private readonly string replacement;

        private AliasReferenceReplacer(string alias, string replacement)
        {
            this.alias = alias;
            this.replacement = replacement;
        }

        public static Expression Replace(Expression expression, string alias, string replacement)
        {
            return new AliasReferenceReplacer(alias, replacement).Visit(expression);
        }

        protected override Expression VisitColumn(SqlColumnExpression columnExpression)
        {
            if (columnExpression.SelectAlias == this.alias)
            {
                return columnExpression.ChangeAlias(this.replacement);
            }

            return base.VisitColumn(columnExpression);
        }
    }
}