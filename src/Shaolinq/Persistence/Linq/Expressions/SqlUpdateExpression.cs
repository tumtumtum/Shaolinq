// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

using System.Linq.Expressions;
using System.Collections.ObjectModel;

namespace Shaolinq.Persistence.Linq.Expressions
{
	public class SqlUpdateExpression
		: SqlBaseExpression
	{
		public string TableName { get; private set; }
		public Expression Where { get; private set; }
		public ReadOnlyCollection<Expression> Assignments { get; private set; }
		
		public override ExpressionType NodeType
		{
			get
			{
				return (ExpressionType)SqlExpressionType.Update;
			}
		}

		public SqlUpdateExpression(string tableName, ReadOnlyCollection<Expression> assignments, Expression where)
			: base(typeof(void))
		{
			this.TableName = tableName;
			this.Assignments = assignments;
			this.Where = where;
		}
	}
}