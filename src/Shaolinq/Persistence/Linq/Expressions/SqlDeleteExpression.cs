// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)

using System.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Expressions
{
	public class SqlDeleteExpression
		: SqlBaseExpression
	{
		public Expression Source { get; }
		public Expression Where { get; set; }
		public override ExpressionType NodeType => (ExpressionType)SqlExpressionType.Delete;

		public SqlDeleteExpression(Expression source, Expression where)
			: base(typeof(void))
		{
			this.Source = source;
			this.Where = where;
		}
	}
}
