// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Expressions
{
	public class SqlPragmaExpression
		: SqlBaseExpression
	{
		public string Directive { get; private set; }
		public override ExpressionType NodeType { get { return (ExpressionType)SqlExpressionType.Pragma; } }

		public SqlPragmaExpression(string directive)
			: base(typeof(void))
		{
			this.Directive = directive;
		}
	}
}
