// Copyright (c) 2007-2014 Thong Nguyen (tumtumtum@gmail.com)

using System.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Expressions
{
	public class SqlCreateTypeExpression
		: SqlBaseExpression
	{
		public bool IfNotExist { get; private set; }
		public Expression SqlType { get; private set; }
		public Expression AsExpression { get; private set; }
		public override ExpressionType NodeType { get { return (ExpressionType)SqlExpressionType.CreateType; } }

		public SqlCreateTypeExpression(Expression sqlType, Expression asExpression, bool ifNotExist)
			: base(typeof(void))
		{
			this.SqlType = sqlType;
			this.AsExpression = asExpression;
			this.IfNotExist = ifNotExist;
		}
	}
}
