// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

using System.Collections.ObjectModel;
using System.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Expressions
{
	public class SqlInsertIntoExpression
		: SqlBaseExpression
	{
		public string TableName { get; set; }
		public ReadOnlyCollection<string> ColumnNames { get; set; }
		public string ReturningAutoIncrementColumnName { get; set; }
		public ReadOnlyCollection<Expression> ValueExpressions { get; set; }

		public override ExpressionType NodeType
		{
			get
			{
				return (ExpressionType)SqlExpressionType.InsertInto;
			}
		}

		public SqlInsertIntoExpression(string tableName, ReadOnlyCollection<string> columnNames, string returningAutoIncrementColumnName, ReadOnlyCollection<Expression> valueExpressions)
			: base(typeof(void))
		{
			this.TableName = tableName;
			this.ColumnNames = columnNames;
			this.ReturningAutoIncrementColumnName = returningAutoIncrementColumnName;
			this.ValueExpressions = valueExpressions;
		}
	}
}
