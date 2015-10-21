// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System.Linq.Expressions;
using Shaolinq.Persistence.Linq.Expressions;

namespace Shaolinq.SqlServer
{
	public class BitBooleanExpression
		: SqlBaseExpression
	{
		public Expression Expression { get; }
		public override bool CanReduce => true;
		public override ExpressionType NodeType => ExpressionType.Extension;

		public BitBooleanExpression(bool value, bool nullable = false)
			: this(Expression.Constant(value, nullable ? typeof(bool?) : typeof(bool)))
		{
		}

		public BitBooleanExpression(Expression expression)
			: base(expression.Type)
		{
			this.Expression = expression;
		}

		public override Expression Reduce()
		{
			return this.Expression;
		}

		public static BitBooleanExpression Coerce(Expression expression)
		{
			var nullable = expression.Type == typeof(bool?);

			var retval = new BitBooleanExpression(Expression.Condition(expression, new BitBooleanExpression(Expression.Constant(true, nullable ? typeof(bool?) : typeof(bool))), new BitBooleanExpression(Expression.Constant(false, nullable ? typeof(bool?) : typeof(bool)))));

			if (nullable)
			{
				retval = new BitBooleanExpression(Expression.Condition(Expression.Equal(retval, Expression.Constant(null)), Expression.Constant(null, typeof(bool?)), retval));
			}

			return retval;
		}
	}
}
