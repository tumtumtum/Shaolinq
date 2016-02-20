// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Expressions
{
	/// <summary>
	/// A SqlColumnDeclaraction represents the part in parenthesis in the following select statement: SELECT (expression as columnname).
	/// </summary>
	public class SqlColumnDeclaration
	{
		public string Name { get; }
		public bool NoOptimise { get; }
		public Expression Expression { get; }

		public SqlColumnDeclaration(string name, Expression expression, bool noOptimise = false)
		{
			this.Name = name;
			this.Expression = expression;
			this.NoOptimise = noOptimise;
		}

		public SqlColumnDeclaration ReplaceExpression(Expression expression)
		{
			return new SqlColumnDeclaration(this.Name, expression, this.NoOptimise);
		}
	}
}
