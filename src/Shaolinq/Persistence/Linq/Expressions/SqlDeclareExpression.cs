// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System.Collections.Generic;

namespace Shaolinq.Persistence.Linq.Expressions
{
	public class SqlDeclareExpression 
		: SqlBaseExpression
	{
		public IReadOnlyList<SqlVariableDeclarationExpression> VariableDeclarations { get; set; }

		public SqlDeclareExpression(IEnumerable<SqlVariableDeclarationExpression> variables)
			: this(variables.ToReadOnlyCollection())
		{
		}

		public SqlDeclareExpression(IReadOnlyList<SqlVariableDeclarationExpression> variableDeclarations)
			: base(typeof(void))
		{
			this.VariableDeclarations = variableDeclarations;
		}
	}
}