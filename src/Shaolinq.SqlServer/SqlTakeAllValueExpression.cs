// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System.Linq.Expressions;
using Shaolinq.Persistence.Linq.Expressions;

namespace Shaolinq.SqlServer
{
	public class SqlTakeAllValueExpression
		: SqlBaseExpression
	{
		public override ExpressionType NodeType => ExpressionType.Extension;

		public SqlTakeAllValueExpression()
			: base(typeof(int))
		{
		}
	}
}
