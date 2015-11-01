// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Expressions
{
	public class SqlScalarEpression
		: SqlSubqueryExpression
	{
		public override ExpressionType NodeType => (ExpressionType)SqlExpressionType.Scalar;

		public SqlScalarEpression(Type type, SqlSelectExpression select)
			: base(type, select)
		{
		}

		public virtual SqlScalarEpression ChangeSelect(SqlSelectExpression select)
		{
			return new SqlScalarEpression(this.Type, select);
		}
	}
}