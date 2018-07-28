// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)

using System.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Expressions
{
	public class SqlKeywordExpression
		: SqlBaseExpression
	{
		public string Name { get; }
		public override ExpressionType NodeType => (ExpressionType)SqlExpressionType.Keyword;

		public SqlKeywordExpression(string name)
			: base(typeof(void))
		{
			this.Name = name;
		}
	}
}