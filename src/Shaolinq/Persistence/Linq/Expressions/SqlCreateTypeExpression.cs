// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Expressions
{
	public class SqlCreateTypeExpression
		: SqlBaseExpression
	{
		public bool IfNotExist { get; }
		public Expression SqlType { get; }
		public Expression AsExpression { get; }
		public override ExpressionType NodeType => (ExpressionType)SqlExpressionType.CreateType;

		public SqlCreateTypeExpression(Expression sqlType, Expression asExpression, bool ifNotExist)
			: base(typeof(void))
		{
			this.SqlType = sqlType;
			this.AsExpression = asExpression;
			this.IfNotExist = ifNotExist;
		}
	}
}
