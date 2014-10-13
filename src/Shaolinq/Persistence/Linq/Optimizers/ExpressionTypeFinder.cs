using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Shaolinq.Persistence.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Optimizers
{
	public class ExpressionTypeFinder
		: SqlExpressionVisitor
	{
		private Expression result;
		private readonly ExpressionType typeToFind;

		private ExpressionTypeFinder(ExpressionType typeToFind)
		{
			this.typeToFind = typeToFind;
		}

		public Expression Find(Expression expression, ExpressionType type)
		{
			var finder = new ExpressionTypeFinder(type);

			finder.Visit(expression);

			return finder.result;
		}

		protected override System.Linq.Expressions.Expression Visit(System.Linq.Expressions.Expression expression)
		{
			if (expression.NodeType == this.typeToFind)
			{
				this.result = expression;

				return expression;
			}

			return base.Visit(expression);
		}
	}
}
