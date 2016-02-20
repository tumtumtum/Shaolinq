// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Expressions
{
	public class SqlPragmaExpression
		: SqlBaseExpression
	{
		public string Directive { get; }
		public override ExpressionType NodeType => (ExpressionType)SqlExpressionType.Pragma;

		public SqlPragmaExpression(string directive)
			: base(typeof(void))
		{
			this.Directive = directive;
		}
	}
}
