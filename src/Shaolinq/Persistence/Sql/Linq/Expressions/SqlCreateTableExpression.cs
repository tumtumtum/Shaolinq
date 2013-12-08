// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq.Expressions;

namespace Shaolinq.Persistence.Sql.Linq.Expressions
{
	public class SqlCreateTableExpression
		: SqlBaseExpression
	{
		public string TableName { get; set; }
		public ReadOnlyCollection<Expression> TableConstraints { get; private set; }
		public ReadOnlyCollection<Expression> ColumnDefinitionExpressions { get; private set; }

		public override ExpressionType NodeType
		{
			get
			{
				return (ExpressionType)SqlExpressionType.CreateTable;
			}
		}

		public SqlCreateTableExpression(string tableName, IList<Expression> columnExpressions, IList<Expression> tableConstraintExpressions)
			: base(typeof(void))
		{
			this.TableName = tableName;
			this.TableConstraints = new ReadOnlyCollection<Expression>(tableConstraintExpressions);
			this.ColumnDefinitionExpressions = new ReadOnlyCollection<Expression>(columnExpressions);
		}
	}
}
