// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)

using System.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Expressions
{
	public partial class SqlExpressionHasher
		: SqlExpressionVisitor
	{
		private int hashCode;
		private readonly SqlExpressionComparerOptions options;

		private SqlExpressionHasher(SqlExpressionComparerOptions options)
		{
			this.options = options;
		}

		public static int Hash(Expression expression)
		{
			return Hash(expression, SqlExpressionComparerOptions.None);
		}

		public static int Hash(Expression expression, SqlExpressionComparerOptions options)
		{
			if (expression == null)
			{
				return 0;
			}

			var hasher = new SqlExpressionHasher(options);

			hasher.Visit(expression);

			return hasher.hashCode;
		}

		protected override Expression Visit(Expression expression)
		{
			if (expression != null)
			{
				this.hashCode ^= (int)expression.NodeType << 17;
				this.hashCode ^= expression.Type.GetHashCode();
			}

			return base.Visit(expression);
		}
		
		protected override Expression VisitConstant(ConstantExpression constantExpression)
		{
			var type = constantExpression.Type;

			if ((this.options & SqlExpressionComparerOptions.IgnoreConstants) != 0)
			{
				return constantExpression;
			}

			if (type.IsValueType)
			{
				if (constantExpression.Value != null)
				{
					this.hashCode ^= constantExpression.Value.GetHashCode();
				}
			}
			else if (typeof(Expression).IsAssignableFrom(constantExpression.Type))
			{
				Visit((Expression)constantExpression.Value);
			}
			else if (type == typeof(string))
			{
				this.hashCode ^= constantExpression.Value?.GetHashCode() ?? 0;
			}

			return constantExpression;
		}

		protected override Expression VisitConstantPlaceholder(SqlConstantPlaceholderExpression constantPlaceholder)
		{
			this.hashCode ^= constantPlaceholder.Index;

			if ((this.options & SqlExpressionComparerOptions.IgnoreConstantPlaceholders) != 0)
			{
				return constantPlaceholder;
			}

			return base.VisitConstantPlaceholder(constantPlaceholder);
		}
	}
}
