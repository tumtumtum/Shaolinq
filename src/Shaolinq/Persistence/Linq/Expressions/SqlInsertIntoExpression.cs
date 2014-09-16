// Copyright (c) 2007-2014 Thong Nguyen (tumtumtum@gmail.com)

using System.Collections.ObjectModel;
using System.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Expressions
{
	public class SqlInsertIntoExpression
		: SqlBaseExpression
	{
		public string TableName { get; private set; }
		public ReadOnlyCollection<string> ColumnNames { get; private set; }
		public ReadOnlyCollection<string> ReturningAutoIncrementColumnNames { get; private set; }
		public ReadOnlyCollection<Expression> ValueExpressions { get; private set; }

		public override ExpressionType NodeType
		{
			get
			{
				return (ExpressionType)SqlExpressionType.InsertInto;
			}
		}

		public SqlInsertIntoExpression(string tableName, ReadOnlyCollection<string> columnNames, ReadOnlyCollection<string> returningAutoIncrementColumnNames, ReadOnlyCollection<Expression> valueExpressions)
			: base(typeof(void))
		{
			this.TableName = tableName;
			this.ColumnNames = columnNames;
			this.ReturningAutoIncrementColumnNames = returningAutoIncrementColumnNames;
			this.ValueExpressions = valueExpressions;
		}
	}
}
