﻿using System.Linq.Expressions;

namespace Shaolinq.Persistence.Sql.Linq.Expressions
{
	public class SqlDeleteExpression
		: SqlBaseExpression
	{
		public string TableName { get; private set; }
		public string Alias { get; private set; }
		public Expression Where { get; private set; }

		public override ExpressionType NodeType
		{
			get
			{
				return (ExpressionType)SqlExpressionType.Delete;
			}
		}

		public SqlDeleteExpression(string tableName, string alias, Expression where)
			: base(typeof(void))
		{
			this.Where = where;
			this.Alias = alias;
			this.TableName = tableName;
		}
	}
}