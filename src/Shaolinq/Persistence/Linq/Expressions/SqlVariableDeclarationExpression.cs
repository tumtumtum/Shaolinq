// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Expressions
{
	public class SqlVariableDeclarationExpression
		: SqlBaseExpression
	{
		public string Name { get; }
		public override ExpressionType NodeType => (ExpressionType)SqlExpressionType.VariableDeclaration;

		public SqlVariableDeclarationExpression(Type type, string name)
			: base(type)
		{
			this.Name = name;
		}
	}
}