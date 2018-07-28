// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)

using System.Linq.Expressions;
using Shaolinq.Persistence.Linq.Expressions;

namespace Shaolinq.SqlServer
{
	/// <summary>
	/// Represents a boolean data type in SQL which is 1 or 0.
	/// </summary>
	/// <remarks>
	/// <see cref="SqlServerBooleanNormalizer"/> automatically adds comparisons to convert BitBooleans 
	/// values into logical true/false when needed for joins, where conditions, etc
	/// </remarks>
	public class BitBooleanExpression
		: SqlBaseExpression
	{
		public Expression Expression { get; }
		public override bool CanReduce => true;
		public override ExpressionType NodeType => ExpressionType.Extension;

		public BitBooleanExpression(bool value, bool nullable = false)
			: this(Constant(value, nullable ? typeof(bool?) : typeof(bool)))
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

			var retval = new BitBooleanExpression(Condition(expression, new BitBooleanExpression(Constant(true, nullable ? typeof(bool?) : typeof(bool))), new BitBooleanExpression(Constant(false, nullable ? typeof(bool?) : typeof(bool)))));

			if (nullable)
			{
				retval = new BitBooleanExpression(Condition(Equal(retval, Constant(null)), Constant(null, typeof(bool?)), retval));
			}

			return retval;
		}
	}
}
