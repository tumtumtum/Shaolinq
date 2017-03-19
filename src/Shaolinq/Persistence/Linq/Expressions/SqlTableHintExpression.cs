// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Expressions
{
	public class SqlTableHintExpression
		: SqlBaseExpression
	{
		public bool TableLock { get; }
		public override ExpressionType NodeType => (ExpressionType)SqlExpressionType.TableHint;

		public SqlTableHintExpression(bool tableLock)
			: base(typeof(void))
		{
			this.TableLock = tableLock;
		}
	}
}