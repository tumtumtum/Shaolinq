// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Expressions
{
	public class SqlScalarExpression
		: SqlSubqueryExpression
	{
		public override ExpressionType NodeType => (ExpressionType)SqlExpressionType.Scalar;

		public SqlScalarExpression(Type type, SqlSelectExpression select)
			: base(type, select)
		{
		}

		public virtual SqlScalarExpression ChangeSelect(SqlSelectExpression select)
		{
			return new SqlScalarExpression(this.Type, select);
		}
	}
}